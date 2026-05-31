using Content.Server.Body.Components;
using Content.Server.Ghost.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Components;
using Content.Server._VG.DelayedDeath;
using Content.Shared._VG.Surgery.Body.Organs;
using Content.Shared._VG.Surgery.Body;

namespace Content.Server.Body.Systems;

public sealed class BrainSystem : SharedBrainSystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnOrganAdded(Entity<BrainComponent> ent, ref OrganAddedToBodyEvent args)
    {
        base.OnOrganAdded(ent, ref args);

        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.Body))
            return;

        if (!CheckOtherBrains(args.Body))
        {
            RemComp<DebrainedComponent>(args.Body);
            if (_bodySystem.TryGetBodyOrganEntityComps<HeartComponent>(args.Body, out var _))
                RemComp<DelayedDeathComponent>(args.Body);
        }
    }

    protected override void OnOrganRemoved(Entity<BrainComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        base.OnOrganRemoved(ent, ref args);

        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(args.OldBody))
            return;

        ent.Comp.Active = false;
        
        if (!CheckOtherBrains(args.OldBody))
        {
            EnsureComp<DebrainedComponent>(args.OldBody);
            EnsureComp<DelayedDeathComponent>(args.OldBody);
        }
    }

    private bool CheckOtherBrains(EntityUid entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body))
            return false;

        if (TryComp<BrainComponent>(entity, out var bodyBrain) && bodyBrain.Active)
            return true;

        foreach (var (organ, _) in _bodySystem.GetBodyOrgans(entity, body))
        {
            if (TryComp<BrainComponent>(organ, out var brain) && brain.Active)
                return true;
        }

        return false;
    }
}