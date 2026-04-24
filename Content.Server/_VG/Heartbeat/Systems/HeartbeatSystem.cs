using Content.Server._VG.Heartbeat.Components;
using Content.Shared._VG.Heartbeat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._VG.Heartbeat.Systems;

public sealed class HeartbeatSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float MinimumCooldown = 0.5f;
    private const float MaximumCooldown = 3f;

    private static readonly SoundSpecifier HeartbeatSound =
        new SoundPathSpecifier("/Audio/_VG/Effects/heartbeat.ogg", AudioParams.Default.WithVolume(-3f));

    private static readonly HashSet<ICommonSession> DisabledSessions = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CritHeartbeatComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ActiveHeartbeatComponent, DamageChangedEvent>(OnDamage);
        SubscribeNetworkEvent<HeartbeatOptionsChangedEvent>(OnOptionsChanged);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => DisabledSessions.Clear());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveHeartbeatComponent>();

        while (query.MoveNext(out var uid, out var activeHeartbeat))
        {
            if (_timing.CurTime < activeHeartbeat.NextHeartbeatTime)
                continue;

            if (IsDisabledByClient(uid))
                continue;

            _audio.PlayGlobal(HeartbeatSound, uid, AudioParams.Default.WithPitchScale(activeHeartbeat.Pitch));
            SetNextTime(activeHeartbeat);
        }
    }

    private void OnMobStateChanged(Entity<CritHeartbeatComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
        {
            RemComp<ActiveHeartbeatComponent>(ent);
            return;
        }

        var activeHeartbeat = EnsureComp<ActiveHeartbeatComponent>(ent);
        TryCalculateCurrentState((ent.Owner, activeHeartbeat));
        SetNextTime(activeHeartbeat);
    }

    private void OnDamage(Entity<ActiveHeartbeatComponent> ent, ref DamageChangedEvent args)
    {
        TryCalculateCurrentState(ent, args.Damageable);
    }

    /// <summary>
    /// Calculates pitch and cooldown based on total damage.
    /// More damage → slower heartbeat and deeper sound.
    /// </summary>
    private bool TryCalculateCurrentState(Entity<ActiveHeartbeatComponent> ent, DamageableComponent? damageable = null)
    {
        if (!Resolve(ent.Owner, ref damageable))
            return false;

        var totalDamage = damageable.TotalDamage.Float();

        var pitch = Math.Min(1f, 100f / totalDamage);
        var excess = Math.Max(0f, totalDamage - 100f);
        var cooldownSeconds = MinimumCooldown + (excess / 100f) * (MaximumCooldown - MinimumCooldown);

        ent.Comp.Pitch = pitch;
        ent.Comp.NextHeartbeatCooldown = TimeSpan.FromSeconds(cooldownSeconds);

        return true;
    }

    /// <summary>
    /// Sets the absolute game time for the next heartbeat.
    /// </summary>
    private void SetNextTime(ActiveHeartbeatComponent component)
    {
        component.NextHeartbeatTime = _timing.CurTime + component.NextHeartbeatCooldown;
    }

    private bool IsDisabledByClient(EntityUid player)
    {
        if (!_player.TryGetSessionByEntity(player, out var session))
            return true;

        return DisabledSessions.Contains(session);
    }

    private static void OnOptionsChanged(HeartbeatOptionsChangedEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Enabled)
            DisabledSessions.Remove(args.SenderSession);
        else
            DisabledSessions.Add(args.SenderSession);
    }
}