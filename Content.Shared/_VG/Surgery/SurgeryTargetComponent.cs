using Robust.Shared.GameStates;

namespace Content.Shared._VG.Surgery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanOperate = true;

    [DataField, AutoNetworkedField]
    public string? DollPath = "/Textures/_VG/Interface/Targeting/Status/Human";
}