using Content.Shared.ADT.MiningShop;
using Robust.Server.GameObjects;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.ADT.Droppods.EntitySystems;

namespace Content.Server.ADT.MiningShop;

public sealed class MiningShopSystem : SharedMiningShopSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DroppodSystem _droppod = default!;

    protected override void OnVendBui(Entity<MiningShopComponent> vendor, ref MiningShopBuiMsg args)
    {
        base.OnVendBui(vendor, ref args);
        var msg = new MiningShopRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }

    protected override void OnRemoveItemBui(Entity<MiningShopComponent> vendor, ref MiningShopRemoveItemBuiMsg args)
    {
        base.OnRemoveItemBui(vendor, ref args);
        var msg = new MiningShopRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }

    protected override void OnClearCartBui(Entity<MiningShopComponent> vendor, ref MiningShopClearCartBuiMsg args)
    {
        base.OnClearCartBui(vendor, ref args);
        var msg = new MiningShopRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }

    protected override void OnVendBuiExpress(Entity<MiningShopComponent> vendor, ref MiningShopExpressDeliveryBuiMsg args)
    {
        var actor = args.Actor;

        if (!vendor.Comp.OrdersByUser.TryGetValue(actor, out var userOrders) || userOrders.Count == 0)
            return;

        uint totalCost = 0;
        foreach (var entry in userOrders)
        {
            totalCost += entry.Price ?? 0;
        }

        if (!TryComp(actor, out TransformComponent? xform))
            return;

        var idCard = _miningPoints.TryFindIdCard(actor);
        if (idCard == null)
            return;

        // VG-Tweak: explicitly check for null component
        var pointsComp = idCard.Value.Comp;
        if (pointsComp == null)
            return;

        if (pointsComp.Points < totalCost)
            return;

        if (!_miningPoints.RemovePoints(idCard.Value, totalCost))
            return;

        List<EntProtoId> ids = userOrders.Select(entry => entry.Id).ToList();
        _droppod.CreateDroppod(xform.Coordinates, ids);

        vendor.Comp.OrdersByUser.Remove(actor);
        Dirty(vendor.Owner, vendor.Comp);

        var msg = new MiningShopRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }
}