using Content.Shared._VG.Mood;

namespace Content.Shared._VG.Mood;

/// <summary>
/// Event raised when an entity's mood threshold changes.
/// </summary>
public sealed class MoodThresholdChangedEvent : EntityEventArgs
{
    public readonly MoodThreshold OldThreshold;
    public readonly MoodThreshold NewThreshold;

    public MoodThresholdChangedEvent(MoodThreshold oldT, MoodThreshold newT)
    {
        OldThreshold = oldT;
        NewThreshold = newT;
    }
}