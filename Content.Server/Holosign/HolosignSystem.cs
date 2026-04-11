using Content.Shared.Examine;
using Content.Shared.Coordinates.Helpers;
using Content.Server.PowerCell;
using Content.Shared.Interaction;
using Content.Shared.Power.Components;
using Content.Shared.Storage;
using System.Linq;
using Content.Server.Holosign; // VG-Tweak

namespace Content.Server.Holosign;

public sealed class HolosignSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HolosignProjectorComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
        SubscribeLocalEvent<HolosignProjectorComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(EntityUid uid, HolosignProjectorComponent component, ExaminedEvent args)
    {
        _powerCell.TryGetBatteryFromSlot(uid, out var battery);
        var charges = UsesRemaining(component, battery);
        var maxCharges = MaxUses(component, battery);

        using (args.PushGroup(nameof(HolosignProjectorComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
            {
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
            }
        }
    }

    private void OnBeforeInteract(EntityUid uid, HolosignProjectorComponent component, BeforeRangedInteractEvent args)
    {
        if (
            args.Handled
            || !args.CanReach
            || HasComp<StorageComponent>(args.Target)
        )
            return;

        var entities = _lookup.GetEntitiesInRange(args.ClickLocation.SnapToGrid(EntityManager), .1f).ToList().Where(e => HasComp<HolosignComponent>(e));
        if (
            entities.Any()
            || !_powerCell.TryUseCharge(uid, component.ChargeUse, user: args.User)
        )
            return;

        var holoUid = Spawn(component.SignProto, args.ClickLocation.SnapToGrid(EntityManager));
        var xform = Transform(holoUid);
        
        // VG: Используем флаг anchorOnSpawn
        if (!xform.Anchored && component.AnchorOnSpawn)
            _transform.AnchorEntity(holoUid, xform);

        args.Handled = true;
    }

    private int UsesRemaining(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int)(battery.CurrentCharge / component.ChargeUse);
    }

    private int MaxUses(HolosignProjectorComponent component, BatteryComponent? battery = null)
    {
        if (battery == null ||
            component.ChargeUse == 0f) return 0;

        return (int)(battery.MaxCharge / component.ChargeUse);
    }
}