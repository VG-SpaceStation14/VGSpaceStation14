using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._VG.EventDrop;

[RegisterComponent, NetworkedComponent]
public sealed partial class EventDropComponent : Component
{
    // Список прототипов для сброса
    [DataField]
    public List<EntProtoId> PreparedItems = new();
}