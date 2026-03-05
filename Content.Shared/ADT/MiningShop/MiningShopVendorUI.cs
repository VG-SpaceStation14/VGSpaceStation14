using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MiningShop;

[Serializable, NetSerializable]
public enum MiningShopUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiningShopBuiMsg(MiningShopEntry entry) : BoundUserInterfaceMessage
{
    public readonly MiningShopEntry Entry = entry;
}

// VG-Tweak Start: new messages for removing items
[Serializable, NetSerializable]
public sealed class MiningShopRemoveItemBuiMsg(MiningShopEntry entry) : BoundUserInterfaceMessage
{
    public readonly MiningShopEntry Entry = entry;
}

[Serializable, NetSerializable]
public sealed class MiningShopClearCartBuiMsg() : BoundUserInterfaceMessage
{
}
// VG-Tweak End

[Serializable, NetSerializable]
public sealed class MiningShopExpressDeliveryBuiMsg() : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class MiningShopRefreshBuiMsg : BoundUserInterfaceMessage;