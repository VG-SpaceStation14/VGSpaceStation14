using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Pointing;

namespace Content.Shared.Body.Systems;

public abstract class SharedBrainSystem : EntitySystem
{
    [Dependency] protected readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainComponent, OrganAddedToBodyEvent>(OnOrganAdded);
        SubscribeLocalEvent<BrainComponent, OrganRemovedFromBodyEvent>(OnOrganRemoved);
        SubscribeLocalEvent<BrainComponent, PointAttemptEvent>(OnPointAttempt);
    }

    // VG-Tweak Start
    protected virtual void OnOrganAdded(Entity<BrainComponent> ent, ref OrganAddedToBodyEvent args)
    {
        HandleMindTransfer(args.Body, ent);
    }
    // VG-Tweak End

    // VG-Tweak Start
    protected virtual void OnOrganRemoved(Entity<BrainComponent> ent, ref OrganRemovedFromBodyEvent args)
    {
        HandleMindTransfer(ent, args.OldBody);
    }
    // VG-Tweak End

    protected void HandleMindTransfer(EntityUid newEntity, EntityUid oldEntity)
    {
        if (TerminatingOrDeleted(newEntity) || TerminatingOrDeleted(oldEntity))
            return;

        EnsureComp<MindContainerComponent>(newEntity);
        EnsureComp<MindContainerComponent>(oldEntity);

        if (!_mindSystem.TryGetMind(oldEntity, out var mindId, out var mind))
            return;

        _mindSystem.TransferTo(mindId, newEntity, mind: mind);
    }

    private void OnPointAttempt(Entity<BrainComponent> ent, ref PointAttemptEvent args)
    {
        args.Cancel();
    }
}