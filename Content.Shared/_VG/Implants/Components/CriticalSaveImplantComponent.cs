using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._VG.Implants.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CriticalSaveImplantComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Duration = 5f;

    [DataField, AutoNetworkedField]
    public bool IsActive = false;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ExpireTime;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ActivateTime;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? SavedDamage;

    [DataField, AutoNetworkedField]
    public float CurrentHeartbeatCooldown;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextHeartbeatTime;
}