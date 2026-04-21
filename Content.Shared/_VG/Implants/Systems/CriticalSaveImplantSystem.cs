using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared._VG.Implants.Components;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared._VG.Implants.Systems;

public sealed class CriticalSaveImplantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;

    private readonly HashSet<EntityUid> _activatedImplants = new();

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

            if (comp.IsActive || _activatedImplants.Contains(uid))
                continue;

            _activatedImplants.Add(uid);

            if (TryComp<DamageableComponent>(args.Target, out var damageable))
            {
                comp.SavedDamage = new DamageSpecifier
                {
                    DamageDict = new Dictionary<string, FixedPoint2>(damageable.Damage.DamageDict)
                };
            }

            comp.IsActive = true;
            comp.ExpireTime = _timing.CurTime + TimeSpan.FromSeconds(comp.Duration);

            _damageable.SetAllDamage(args.Target, FixedPoint2.Zero);

            _popup.PopupEntity(Loc.GetString("critical-save-implant-activated"), args.Target, args.Target, PopupType.Medium);
            Dirty(uid, comp);

            return;
        }
    }

    private void OnComponentRemove(Entity<CriticalSaveImplantComponent> ent, ref ComponentRemove args)
    {
        _activatedImplants.Remove(ent);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CriticalSaveImplantComponent, SubdermalImplantComponent>();

        while (query.MoveNext(out var uid, out var comp, out var implant))
        {
            if (!comp.IsActive || comp.ExpireTime == null || curTime < comp.ExpireTime)
                continue;

            if (implant.ImplantedEntity is { } target && comp.SavedDamage != null)
            {
                _damageable.ChangeDamage((target, null), comp.SavedDamage, ignoreResistances: true);
                _popup.PopupEntity(Loc.GetString("critical-save-implant-expired"), target, target, PopupType.MediumCaution);
            }

            _activatedImplants.Remove(uid);
            QueueDel(uid);
        }
    }
}