using Robust.Shared.Physics.Events;

namespace Content.Shared._VG.EntityFilterBarrier;

public abstract partial class SharedEntityFilterBarrierSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityFilterBarrierComponent, PreventCollideEvent>(OnPreventCollide);
    }

    protected virtual void OnPreventCollide(EntityUid uid, EntityFilterBarrierComponent component, ref PreventCollideEvent args)
    {
        var protoId = MetaData(args.OtherEntity).EntityPrototype?.ID;

        if (protoId == null || !component.BlockedPrototypes.Contains(protoId))
            args.Cancelled = true;
    }
}