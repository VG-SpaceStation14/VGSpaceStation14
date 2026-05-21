using Content.Shared._VG.BindableLock.Components;
using Content.Shared.Access.Components;
using Content.Shared.Examine;

namespace Content.Server._VG.BindableLock.Systems;

public sealed class BindableLockExamineSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BindableLockComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, BindableLockComponent component, ExaminedEvent args)
    {
        if (component.CanBind)
            return;

        if (!TryComp<AccessReaderComponent>(uid, out var accessReader) || accessReader.AccessKeys.Count == 0)
            return;

        args.PushMarkup(Loc.GetString("examine-bindable-lock-bound"));
    }
}