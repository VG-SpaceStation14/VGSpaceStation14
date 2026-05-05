using Content.Server.Chat.Managers;
using Content.Server.Wagging;
using Content.Shared._VG.Mood;
using Content.Server._VG.Mood;
using Content.Shared._VG.Wagging;
using Content.Shared.Wagging;
using Content.Shared.Toggleable;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Popups;

namespace Content.Server._VG.Wagging;

public sealed class AutoWaggingSystem : EntitySystem
{
    [Dependency] private readonly WaggingSystem _wagging = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const float CheckInterval = 3.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoWaggingComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AutoWaggingComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnStartup(EntityUid uid, AutoWaggingComponent component, ComponentStartup args)
    {
        component.NextCheckTime = _timing.CurTime + TimeSpan.FromSeconds(CheckInterval);
    }

    private void OnToggleAction(EntityUid uid, AutoWaggingComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        component.AutoWaggingEnabled = !component.AutoWaggingEnabled;

        if (!component.AutoWaggingEnabled)
        {
            if (TryComp<WaggingComponent>(uid, out var wagging) && wagging.Wagging)
            {
                _wagging.TryToggleWagging(uid, wagging);
            }
            
            _popup.PopupEntity(Loc.GetString("auto-wagging-disabled-popup"), uid, uid, PopupType.Medium);
        }
        else
        {
            if (TryComp<MoodComponent>(uid, out var mood) && TryComp<WaggingComponent>(uid, out var wagging))
            {
                var shouldWag = mood.CurrentMoodThreshold >= component.RequiredMood;
                if (shouldWag && !wagging.Wagging)
                {
                    _wagging.TryToggleWagging(uid, wagging);
                }
            }
            
            _popup.PopupEntity(Loc.GetString("auto-wagging-enabled-popup"), uid, uid, PopupType.Medium);
        }

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutoWaggingComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp))
        {
            if (currentTime >= comp.NextCheckTime)
            {
                comp.NextCheckTime = currentTime + TimeSpan.FromSeconds(CheckInterval);
                CheckAndUpdateWagging(uid, comp);
            }
        }
    }

    private void CheckAndUpdateWagging(EntityUid uid, AutoWaggingComponent comp)
    {
        if (!comp.AutoWaggingEnabled)
            return;

        if (!TryComp<WaggingComponent>(uid, out var wagging))
            return;

        if (!TryComp<MoodComponent>(uid, out var mood))
            return;

        var currentThreshold = mood.CurrentMoodThreshold;
        var shouldWag = currentThreshold >= comp.RequiredMood;

        if (shouldWag && !wagging.Wagging)
        {
            _wagging.TryToggleWagging(uid, wagging);
        }
        else if (!shouldWag && comp.DisableWhenBelow && wagging.Wagging)
        {
            _wagging.TryToggleWagging(uid, wagging);
        }
    }
}