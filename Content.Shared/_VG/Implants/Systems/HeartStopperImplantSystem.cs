using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Chat;
using Content.Shared._VG.Implants.Components;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._VG.Implants.Systems;

public sealed class HeartStopperImplantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;

    private readonly HashSet<EntityUid> _reviving = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeartStopperImplantComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<HeartStopperActionEvent>(OnAction);
    }

    private bool TryGetUser(SubdermalImplantComponent implant, out EntityUid user)
    {
        user = default;

        if (implant.ImplantedEntity == null)
            return false;

        user = implant.ImplantedEntity.Value;
        return true;
    }

    private void OnRemove(Entity<HeartStopperImplantComponent> ent, ref ComponentRemove args)
    {
        RemoveAction(ent.Owner, ent.Comp);
        _reviving.Remove(ent.Owner);
    }

    private void EnsureAction(EntityUid uid, HeartStopperImplantComponent comp, EntityUid user)
    {
        if (comp.Action != null)
            return;

        _actions.AddAction(user, ref comp.Action, comp.ActionId);
        comp.User = user;

        Dirty(uid, comp);
    }

    private void RemoveAction(EntityUid uid, HeartStopperImplantComponent comp)
    {
        if (comp.Action == null)
            return;

        _actions.RemoveAction(comp.Action.Value);
        comp.Action = null;
        comp.User = null;

        Dirty(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeartStopperImplantComponent, SubdermalImplantComponent>();

        while (query.MoveNext(out var uid, out var comp, out var implant))
        {
            if (_reviving.Contains(uid))
                continue;

            if (!TryGetUser(implant, out var user))
            {
                RemoveAction(uid, comp);
                continue;
            }

            if (user != comp.User)
            {
                RemoveAction(uid, comp);
                EnsureAction(uid, comp, user);
            }

            if (_net.IsClient)
                continue;

            if (!comp.IsActive || comp.ExpireTime == null || _timing.CurTime < comp.ExpireTime)
                continue;

            PerformRevive(uid, comp, user);
        }
    }

    private void OnAction(HeartStopperActionEvent args)
    {
        var performer = args.Performer;

        var query = EntityQueryEnumerator<HeartStopperImplantComponent, SubdermalImplantComponent>();

        while (query.MoveNext(out var uid, out var comp, out var implant))
        {
            if (!TryGetUser(implant, out var user))
                continue;

            if (implant.ImplantedEntity != performer)
                continue;

            if (comp.IsActive || comp.Charges <= 0)
                return;

            if (_mobState.IsDead(user))
                return;

            if (TryComp<DamageableComponent>(user, out var damageable))
            {
                comp.SavedDamage = new DamageSpecifier
                {
                    DamageDict = new(damageable.Damage.DamageDict)
                };
            }

            _chat.TryEmoteWithChat(
                user,
                "DefaultDeathgasp",
                ChatTransmitRange.Normal,
                forceEmote: true);

            _mobState.ChangeMobState(user, MobState.Dead);

            comp.IsActive = true;
            comp.ExpireTime = _timing.CurTime + TimeSpan.FromSeconds(comp.Duration);
            comp.Charges--;

            _popup.PopupEntity(
                Loc.GetString("heart-stopper-activated", ("charges", comp.Charges)),
                user,
                user);

            Dirty(uid, comp);

            args.Handled = true;
            return;
        }
    }

    private void PerformRevive(EntityUid uid, HeartStopperImplantComponent comp, EntityUid user)
    {
        if (_reviving.Contains(uid))
            return;

        _reviving.Add(uid);

        if (!Exists(user))
        {
            _reviving.Remove(uid);
            return;
        }

        if (_mobState.IsDead(user))
            _mobState.ChangeMobState(user, MobState.Alive);

        if (comp.SavedDamage != null && TryComp<DamageableComponent>(user, out _))
        {
            _damageable.SetAllDamage(user, 0);
            _damageable.TryChangeDamage(user, comp.SavedDamage, true);
        }

        _popup.PopupEntity(
            Loc.GetString("heart-stopper-revived"),
            user,
            user);

        comp.IsActive = false;
        comp.ExpireTime = null;
        comp.SavedDamage = null;

        if (comp.Charges <= 0)
        {
            RemoveAction(uid, comp);
            QueueDel(uid);
        }

        Dirty(uid, comp);
        _reviving.Remove(uid);
    }
}

public sealed partial class HeartStopperActionEvent : InstantActionEvent { }