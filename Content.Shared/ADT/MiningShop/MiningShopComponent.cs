using System.Numerics;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MiningShop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMiningShopSystem))]
public sealed partial class MiningShopComponent : Component
{
    // VG-Tweak Start: change to per-user orders
    /// <summary>
    /// Orders per user. Key is the user EntityUid.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, List<MiningShopEntry>> OrdersByUser = new();
    // VG-Tweak End

    [DataField, AutoNetworkedField]
    public Vector2 MinOffset = new(-0.2f, -0.2f);

    [DataField, AutoNetworkedField]
    public Vector2 MaxOffset = new (0.2f, 0.2f);
}