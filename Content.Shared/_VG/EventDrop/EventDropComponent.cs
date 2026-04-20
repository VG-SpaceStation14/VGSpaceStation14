using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._VG.EventDrop;

[RegisterComponent, NetworkedComponent]
public sealed partial class EventDropComponent : Component
{
    [DataField]
    public List<EntProtoId> PreparedItems = new();
    
    [DataField]
    public string? CurrentPresetName;
}