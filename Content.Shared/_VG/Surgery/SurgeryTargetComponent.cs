using Robust.Shared.GameStates;

namespace Content.Shared._VG.Surgery;

[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryTargetComponent : Component
{
    [DataField]
    public bool CanOperate = true;
}
