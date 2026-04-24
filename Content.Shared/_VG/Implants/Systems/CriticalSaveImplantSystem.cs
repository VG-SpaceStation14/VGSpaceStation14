using Content.Shared._VG.Implants.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._VG.Implants.Systems;

public abstract class SharedCriticalSaveImplantSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly INetManager Net = default!;
    [Dependency] protected readonly SharedChatSystem Chat = default!;

    protected readonly HashSet<EntityUid> ActivatedImplants = new();

    protected const float InitialHeartbeatCooldown = 0.3f;
    protected const float FinalHeartbeatCooldown = 1.5f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<CriticalSaveImplantComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        var query = EntityQueryEnumerator<CriticalSaveImplantComponent, SubdermalImplantComponent>();
        while (query.MoveNext(out var uid, out var comp, out var implant))
        {
            if (implant.ImplantedEntity != args.Target)
                continue;

            if (comp.IsActive || ActivatedImplants.Contains(uid))
                continue;

            ActivatedImplants.Add(uid);

            if (TryComp<DamageableComponent>(args.Target, out var damageable))
            {
                comp.SavedDamage = new DamageSpecifier
                {
                    DamageDict = new Dictionary<string, FixedPoint2>(damageable.Damage.DamageDict)
                };
            }

            comp.IsActive = true;
            comp.ActivateTime = Timing.CurTime;
            comp.ExpireTime = Timing.CurTime + TimeSpan.FromSeconds(comp.Duration);

            // Start heartbeat immediately
            comp.CurrentHeartbeatCooldown = InitialHeartbeatCooldown;
            comp.NextHeartbeatTime = Timing.CurTime;

            Damageable.SetAllDamage(args.Target, FixedPoint2.Zero);

            Chat.TrySendInGameICMessage(
                args.Target,
                Loc.GetString("critical-save-implant-do-message"),
                InGameICChatType.Emote,
                false);

            Popup.PopupEntity(Loc.GetString("critical-save-implant-activated"), args.Target, args.Target, PopupType.Medium);
            Dirty(uid, comp);

            return;
        }
    }

    private void OnComponentRemove(Entity<CriticalSaveImplantComponent> ent, ref ComponentRemove args)
    {
        ActivatedImplants.Remove(ent);
    }

    protected void UpdateHeartbeatCooldown(CriticalSaveImplantComponent comp, TimeSpan curTime)
    {
        if (comp.ActivateTime == null)
            return;

        var elapsed = (float)(curTime - comp.ActivateTime.Value).TotalSeconds;
        var totalDuration = comp.Duration;
        var progress = Math.Clamp(elapsed / totalDuration, 0f, 1f);

        comp.CurrentHeartbeatCooldown = InitialHeartbeatCooldown +
                                        (FinalHeartbeatCooldown - InitialHeartbeatCooldown) * progress;
    }
}