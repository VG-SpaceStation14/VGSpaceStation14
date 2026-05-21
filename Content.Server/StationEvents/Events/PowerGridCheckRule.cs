using System.Threading;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Station.Components;
using Content.Shared._VG.Effects; // VG-Tweak
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random; // VG-Tweak
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.StationEvents.Events;

[UsedImplicitly]
public sealed class PowerGridCheckRule : StationEventSystem<PowerGridCheckRuleComponent>
{
    [Dependency] private readonly ApcSystem _apcSystem = default!;
    // VG-Tweak Start
    [Dependency] private readonly SparksSystem _sparks = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float SparkChanceOff = 0.3f;
    private const float SparkChanceOn = 0.2f;
    private const float PowerOnDelay = 0.1f;
    private const float InitialDelay = 2.0f;
    // VG-Tweak End

    protected override void Started(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        component.AffectedStation = chosenStation.Value;

        var query = AllEntityQuery<ApcComponent, TransformComponent>();
        while (query.MoveNext(out var apcUid, out var apc, out var transform))
        {
            if (apc.MainBreakerEnabled && CompOrNull<StationMemberComponent>(transform.GridUid)?.Station == chosenStation)
                component.Powered.Add(apcUid);
        }

        RobustRandom.Shuffle(component.Powered); // VG-Tweak

        component.NumberPerSecond = Math.Max(1, (int)(component.Powered.Count / component.SecondsUntilOff));
    }

    protected override void Ended(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);

        component.AnnounceCancelToken?.Cancel();
        component.AnnounceCancelToken = new CancellationTokenSource();

        // VG-Tweak Start
        Audio.PlayGlobal(component.EndSound ?? new SoundPathSpecifier("/Audio/Announcements/power_on.ogg"), Filter.Broadcast(), true);
        
        var count = component.Unpowered.Count;
        
        for (var i = 0; i < count; i++)
        {
            var entity = component.Unpowered[i];
            var index = i;
            
            Timer.Spawn(TimeSpan.FromSeconds(InitialDelay + index * PowerOnDelay), () =>
            {
                if (Deleted(entity))
                    return;

                if (TryComp(entity, out ApcComponent? apcComponent))
                {
                    if (!apcComponent.MainBreakerEnabled)
                    {
                        _apcSystem.ApcToggleBreaker(entity, apcComponent);

                        if (_random.Prob(SparkChanceOn))
                        {
                            var xform = Transform(entity);
                            _sparks.DoSparks(xform.Coordinates, minSparks: 2, maxSparks: 4, 
                                minVelocity: 0.5f, maxVelocity: 2f, playSound: true);
                        }
                    }
                }
            }, component.AnnounceCancelToken.Token);
        }
        
        Timer.Spawn(TimeSpan.FromSeconds(InitialDelay + count * PowerOnDelay + 0.5), () =>
        {
            component.Unpowered.Clear();
        }, component.AnnounceCancelToken.Token);
        // VG-Tweak End
    }

    protected override void ActiveTick(EntityUid uid, PowerGridCheckRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        var updates = 0;
        component.FrameTimeAccumulator += frameTime;
        if (component.FrameTimeAccumulator > component.UpdateRate)
        {
            updates = (int)(component.FrameTimeAccumulator / component.UpdateRate);
            component.FrameTimeAccumulator -= component.UpdateRate * updates;
        }

        for (var i = 0; i < updates; i++)
        {
            if (component.Powered.Count == 0)
                break;

            var selected = component.Powered.Pop();
            if (Deleted(selected))
                continue;
                
            if (TryComp<ApcComponent>(selected, out var apcComponent))
            {
                if (apcComponent.MainBreakerEnabled)
                {
                    _apcSystem.ApcToggleBreaker(selected, apcComponent);

                    // VG-Tweak Start
                    if (_random.Prob(SparkChanceOff))
                    {
                        var xform = Transform(selected);
                        _sparks.DoSparks(xform.Coordinates, minSparks: 3, maxSparks: 6, 
                            minVelocity: 1f, maxVelocity: 3f, playSound: true);
                    }
                    // VG-Tweak End
                }
            }
            
            component.Unpowered.Add(selected);
        }
    }
}