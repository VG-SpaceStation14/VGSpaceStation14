using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BrainComponent : Component
{
    [DataField]
    public bool Active = true;
}