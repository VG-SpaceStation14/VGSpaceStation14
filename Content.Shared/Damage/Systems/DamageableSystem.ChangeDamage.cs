using System.Linq;
using Content.Shared._VG.Targeting;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;

namespace Content.Shared.Damage.Systems;

public sealed partial class DamageableSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;

    public void SetDamage(EntityUid uid, DamageableComponent damageable, DamageSpecifier damage)
    {
        damageable.Damage = damage;
        DamageChanged(uid, damageable);
    }

    public void DamageChanged(EntityUid uid, DamageableComponent component, DamageSpecifier? damageDelta = null,
        bool interruptsDoAfters = true, EntityUid? origin = null, bool? canSever = null)
    {
        component.Damage.GetDamagePerGroup(_prototypeManager, component.DamagePerGroup);
        component.TotalDamage = component.Damage.GetTotal();
        Dirty(uid, component);

        if (_appearanceQuery.TryGetComponent(uid, out var appearance) && damageDelta != null)
        {
            var data = new DamageVisualizerGroupData(component.DamagePerGroup.Keys.ToList());
            _appearance.SetData(uid, DamageVisualizerKeys.DamageUpdateGroups, data, appearance);
        }

        RaiseLocalEvent(uid, new DamageChangedEvent(component, damageDelta, interruptsDoAfters, origin, canSever ?? true));
    }

    public DamageSpecifier? TryChangeDamage(EntityUid? uid, DamageSpecifier damage, bool ignoreResistances = false,
        bool interruptsDoAfters = true, DamageableComponent? damageable = null, EntityUid? origin = null,
        bool? canSever = true, bool? canEvade = false, float? partMultiplier = 1.00f, TargetBodyPart? targetPart = null)
    {
        if (!uid.HasValue || !_damageableQuery.Resolve(uid.Value, ref damageable, false))
            return null;

        if (damage.Empty)
            return damage;

        var before = new BeforeDamageChangedEvent(damage, origin, targetPart, canEvade ?? false);
        RaiseLocalEvent(uid.Value, ref before);

        if (before.Cancelled)
            return null;

        var partDamage = new TryChangePartDamageEvent(damage, origin, targetPart, canSever ?? true, canEvade ?? false, partMultiplier ?? 1.00f);
        RaiseLocalEvent(uid.Value, ref partDamage);

        if (partDamage.Evaded || partDamage.Cancelled)
            return null;

        if (!ignoreResistances)
        {
            if (damageable.DamageModifierSetId != null &&
                _prototypeManager.TryIndex<DamageModifierSetPrototype>(damageable.DamageModifierSetId, out var modifierSet))
            {
                damage = DamageSpecifier.ApplyModifierSet(damage, modifierSet);
            }

            var ev = new DamageModifyEvent(damage, origin, targetPart);
            RaiseLocalEvent(uid.Value, ev);
            damage = ev.Damage;

            if (damage.Empty)
                return damage;
        }

        damage = ApplyUniversalAllModifiers(damage);

        var delta = new DamageSpecifier();
        delta.DamageDict.EnsureCapacity(damage.DamageDict.Count);

        var dict = damageable.Damage.DamageDict;
        foreach (var (type, value) in damage.DamageDict)
        {
            if (!dict.TryGetValue(type, out var oldValue))
                continue;

            var newValue = FixedPoint2.Max(FixedPoint2.Zero, oldValue + value);
            if (newValue == oldValue)
                continue;

            dict[type] = newValue;
            delta.DamageDict[type] = newValue - oldValue;
        }

        if (delta.DamageDict.Count > 0)
            DamageChanged(uid.Value, damageable, delta, interruptsDoAfters, origin, canSever);

        return delta;
    }

    public void SetAllDamage(EntityUid uid, DamageableComponent component, FixedPoint2 newValue)
    {
        if (newValue < 0)
            return;

        foreach (var type in component.Damage.DamageDict.Keys)
        {
            component.Damage.DamageDict[type] = newValue;
        }

        DamageChanged(uid, component, new DamageSpecifier());

        if (HasComp<TargetingComponent>(uid))
        {
            foreach (var (part, _) in _body.GetBodyChildren(uid))
            {
                if (!TryComp(part, out DamageableComponent? damageComp))
                    continue;

                SetAllDamage(part, damageComp, newValue);
            }
        }
    }

    public void SetDamageModifierSetId(EntityUid uid, string? damageModifierSetId, DamageableComponent? comp = null)
    {
        if (!_damageableQuery.Resolve(uid, ref comp))
            return;

        comp.DamageModifierSetId = damageModifierSetId;
        Dirty(uid, comp);
    }
}