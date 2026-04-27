using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared._VG.Surgery.Body.Organs;
using Robust.Shared.Containers;

namespace Content.Shared.Body.Systems;

public partial class SharedBodySystem
{
    private void InitializeOrgans()
    {
        SubscribeLocalEvent<OrganComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OrganComponent, OrganEnableChangedEvent>(OnOrganEnableChanged);
    }

    private void OnMapInit(Entity<OrganComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.OnAdd is not null || ent.Comp.OnRemove is not null)
            EnsureComp<OrganEffectComponent>(ent);
    }

    private void AddOrgan(
        Entity<OrganComponent> organEnt,
        EntityUid bodyUid,
        EntityUid parentPartUid)
    {
        organEnt.Comp.Body = bodyUid;
        var addedEv = new OrganAddedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref addedEv);

        if (organEnt.Comp.Body is not null)
        {
            organEnt.Comp.OriginalBody = organEnt.Comp.Body;
            var addedInBodyEv = new OrganAddedToBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref addedInBodyEv);
            var organEnabledEv = new OrganEnableChangedEvent(true);
            RaiseLocalEvent(organEnt, ref organEnabledEv);
        }

        if (TryComp(parentPartUid, out DamageableComponent? damageable)
            && damageable.TotalDamage > 200)
            TrySetOrganUsed(organEnt, true, organEnt.Comp);

        Dirty(organEnt, organEnt.Comp);
    }

    private void RemoveOrgan(Entity<OrganComponent> organEnt, EntityUid parentPartUid)
    {
        var removedEv = new OrganRemovedEvent(parentPartUid);
        RaiseLocalEvent(organEnt, ref removedEv);

        if (organEnt.Comp.Body is { Valid: true } bodyUid)
        {
            organEnt.Comp.OriginalBody = organEnt.Comp.Body;
            var organDisabledEv = new OrganEnableChangedEvent(false);
            RaiseLocalEvent(organEnt, ref organDisabledEv);
            var removedInBodyEv = new OrganRemovedFromBodyEvent(bodyUid, parentPartUid);
            RaiseLocalEvent(organEnt, ref removedInBodyEv);
        }

        if (parentPartUid is { Valid: true }
            && TryComp(parentPartUid, out DamageableComponent? damageable)
            && damageable.TotalDamage > 200)
            TrySetOrganUsed(organEnt, true, organEnt.Comp);

        organEnt.Comp.Body = null;
        Dirty(organEnt, organEnt.Comp);
    }

    private OrganSlot? CreateOrganSlot(Entity<BodyPartComponent?> parentEnt, string slotId)
    {
        if (!Resolve(parentEnt, ref parentEnt.Comp, logMissing: false))
            return null;

        Containers.EnsureContainer<ContainerSlot>(parentEnt, GetOrganContainerId(slotId));
        var slot = new OrganSlot(slotId);
        parentEnt.Comp.Organs.Add(slotId, slot);
        return slot;
    }

    public bool TryCreateOrganSlot(
        EntityUid? parent,
        string slotId,
        [NotNullWhen(true)] out OrganSlot? slot,
        BodyPartComponent? part = null)
    {
        slot = null;

        if (parent is null || !Resolve(parent.Value, ref part, logMissing: false))
            return false;

        Containers.EnsureContainer<ContainerSlot>(parent.Value, GetOrganContainerId(slotId));
        slot = new OrganSlot(slotId);
        return part.Organs.TryAdd(slotId, slot.Value);
    }

    public bool CanInsertOrgan(
        EntityUid partId,
        string slotId,
        BodyPartComponent? part = null)
    {
        return Resolve(partId, ref part) && part.Organs.ContainsKey(slotId);
    }

    public bool CanInsertOrgan(
        EntityUid partId,
        OrganSlot slot,
        BodyPartComponent? part = null)
    {
        return CanInsertOrgan(partId, slot.Id, part);
    }

    public bool InsertOrgan(
        EntityUid partId,
        EntityUid organId,
        string slotId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ, logMissing: false)
            || !Resolve(partId, ref part, logMissing: false)
            || !CanInsertOrgan(partId, slotId, part))
            return false;

        var containerId = GetOrganContainerId(slotId);
        return Containers.TryGetContainer(partId, containerId, out var container)
            && Containers.Insert(organId, container);
    }

    public bool RemoveOrgan(EntityUid organId, OrganComponent? organ = null)
    {
        if (!Containers.TryGetContainingContainer((organId, null, null), out var container))
            return false;

        var parent = container.Owner;
        return HasComp<BodyPartComponent>(parent)
            && Containers.Remove(organId, container);
    }

    public bool AddOrganToFirstValidSlot(
        EntityUid partId,
        EntityUid organId,
        BodyPartComponent? part = null,
        OrganComponent? organ = null)
    {
        if (!Resolve(partId, ref part, logMissing: false)
            || !Resolve(organId, ref organ, logMissing: false))
            return false;

        foreach (var slotId in part.Organs.Keys)
        {
            InsertOrgan(partId, organId, slotId, part, organ);
            return true;
        }

        return false;
    }

    public List<Entity<T, OrganComponent>> GetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity)
        where T : IComponent
    {
        if (!Resolve(entity, ref entity.Comp))
            return new List<Entity<T, OrganComponent>>();

        var query = GetEntityQuery<T>();
        var list = new List<Entity<T, OrganComponent>>(3);
        foreach (var organ in GetBodyOrgans(entity.Owner, entity.Comp))
        {
            if (query.TryGetComponent(organ.Id, out var comp))
                list.Add((organ.Id, comp, organ.Component));
        }

        return list;
    }

    public bool TryGetBodyOrganEntityComps<T>(
        Entity<BodyComponent?> entity,
        [NotNullWhen(true)] out List<Entity<T, OrganComponent>>? comps)
        where T : IComponent
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
        {
            comps = null;
            return false;
        }

        comps = GetBodyOrganEntityComps<T>(entity);
        if (comps.Count != 0)
            return true;

        comps = null;
        return false;
    }

    public bool TrySetOrganUsed(EntityUid organId, bool used, OrganComponent? organ = null)
    {
        if (!Resolve(organId, ref organ) || organ.Used == used)
            return false;

        organ.Used = used;
        Dirty(organId, organ);
        return true;
    }

    private void OnOrganEnableChanged(Entity<OrganComponent> organEnt, ref OrganEnableChangedEvent args)
    {
        if (!organEnt.Comp.CanEnable && args.Enabled)
            return;

        organEnt.Comp.Enabled = args.Enabled;

        if (args.Enabled)
            EnableOrgan(organEnt);
        else
            DisableOrgan(organEnt);

        if (organEnt.Comp.Body is { Valid: true } bodyEnt)
            RaiseLocalEvent(organEnt, new OrganComponentsModifyEvent(bodyEnt, args.Enabled));

        Dirty(organEnt, organEnt.Comp);
    }

    private void EnableOrgan(Entity<OrganComponent> organEnt)
    {
        if (!TryComp(organEnt.Comp.Body, out BodyComponent? body))
            return;

        if (HasComp<EyesComponent>(organEnt))
        {
            var ev = new OrganEnabledEvent(organEnt);
            RaiseLocalEvent(organEnt, ref ev);
        }
    }

    private void DisableOrgan(Entity<OrganComponent> organEnt)
    {
        if (!TryComp(organEnt.Comp.Body, out BodyComponent? body))
            return;

        if (HasComp<EyesComponent>(organEnt))
        {
            var ev = new OrganDisabledEvent(organEnt);
            RaiseLocalEvent(organEnt, ref ev);
        }
    }
}