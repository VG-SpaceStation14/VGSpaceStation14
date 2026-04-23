using Robust.Shared.Serialization;

namespace Content.Shared._VG.Mood;

[Serializable, NetSerializable]
public enum MoodThreshold : ushort
{
    Insane = 1,
    Horrible = 2,
    Terrible = 3,
    Bad = 4,
    Meh = 5,
    Neutral = 6,
    Good = 7,
    Great = 8,
    Exceptional = 9,
    Perfect = 10,
    Dead = 0
}

[Serializable, NetSerializable]
public enum MoodVisuals : byte
{
    CurrentMoodThreshold
}

[Serializable, NetSerializable]
public enum MoodVisualLayers : byte
{
    Mood
}