using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._VG.Implants.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HeartStopperImplantComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Duration = 20f;

    [DataField, AutoNetworkedField]
    public bool IsActive = false;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ExpireTime;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? SavedDamage;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionHeartStopper";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [AutoNetworkedField]
    public EntityUid? User;

    [DataField, AutoNetworkedField]
    public int Charges = 3;
}