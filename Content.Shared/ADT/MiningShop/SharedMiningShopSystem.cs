using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Salvage.Systems;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.ADT.MiningShop;

public abstract class SharedMiningShopSystem : EntitySystem
{
    [Dependency] protected readonly MiningPointsSystem _miningPoints = default!;
    [Dependency] protected readonly INetManager _net = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<MiningShopComponent>(MiningShopUI.Key, subs =>
        {
            subs.Event<MiningShopBuiMsg>(OnVendBui);
            subs.Event<MiningShopRemoveItemBuiMsg>(OnRemoveItemBui);
            subs.Event<MiningShopClearCartBuiMsg>(OnClearCartBui);
            subs.Event<MiningShopExpressDeliveryBuiMsg>(OnVendBuiExpress);
        });
    }

    // VG-Tweak: public accessor for client
    public bool TryGetUserOrders(EntityUid uid, EntityUid user, [NotNullWhen(true)] out List<MiningShopEntry>? orders)
    {
        orders = null;
        return TryComp<MiningShopComponent>(uid, out var comp) && comp.OrdersByUser.TryGetValue(user, out orders);
    }
    // VG-Tweak End

    protected virtual void OnVendBui(Entity<MiningShopComponent> vendor, ref MiningShopBuiMsg args)
    {
        var actor = args.Actor;
        var entry = args.Entry;

        if (entry.Price == null)
            return;

        var idCard = _miningPoints.TryFindIdCard(actor);
        if (idCard == null)
            return;

        var idCardComp = idCard.Value.Comp;
        if (idCardComp == null)
            return;

        uint totalOrdered = 0;
        if (vendor.Comp.OrdersByUser.TryGetValue(actor, out var userOrders))
        {
            foreach (var orderedEntry in userOrders)
            {
                totalOrdered += orderedEntry.Price ?? 0;
            }
        }

        if (idCardComp.Points < totalOrdered + entry.Price.Value)
            return;

        if (!vendor.Comp.OrdersByUser.ContainsKey(actor))
            vendor.Comp.OrdersByUser[actor] = new();

        vendor.Comp.OrdersByUser[actor].Add(entry);
        Dirty(vendor);
    }

    protected virtual void OnRemoveItemBui(Entity<MiningShopComponent> vendor, ref MiningShopRemoveItemBuiMsg args)
    {
        var actor = args.Actor;
        var entry = args.Entry;

        if (!vendor.Comp.OrdersByUser.TryGetValue(actor, out var userOrders))
            return;

        if (userOrders.Remove(entry))
        {
            if (userOrders.Count == 0)
                vendor.Comp.OrdersByUser.Remove(actor);
            Dirty(vendor);
        }
    }

    protected virtual void OnClearCartBui(Entity<MiningShopComponent> vendor, ref MiningShopClearCartBuiMsg args)
    {
        var actor = args.Actor;

        if (vendor.Comp.OrdersByUser.Remove(actor))
            Dirty(vendor);
    }

    protected virtual void OnVendBuiExpress(Entity<MiningShopComponent> vendor, ref MiningShopExpressDeliveryBuiMsg args)
    {
        // To be overridden on server
    }
}

[ByRefEvent]
public record struct ShopVendorBalanceEvent(EntityUid User, uint Balance = 0);