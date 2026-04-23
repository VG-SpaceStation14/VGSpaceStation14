using Robust.Shared.GameStates;
using static Content.Shared._VG.Mood.MoodThreshold;

namespace Content.Shared._VG.Mood;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoodVisualsComponent : Component
{
    [DataField]
    public string? Sprite;

    [DataField]
    public Dictionary<MoodThreshold, string> MoodStates = new();
}