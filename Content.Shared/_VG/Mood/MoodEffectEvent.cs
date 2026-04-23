using Robust.Shared.Serialization;

namespace Content.Shared._VG.Mood;

[Serializable, NetSerializable]
public sealed class MoodEffectEvent : EntityEventArgs
{
    public string EffectId;
    public float EffectModifier;
    public float EffectOffset;

    public MoodEffectEvent(string effectId, float effectModifier = 1f, float effectOffset = 0f)
    {
        EffectId = effectId;
        EffectModifier = effectModifier;
        EffectOffset = effectOffset;
    }
}

[Serializable, NetSerializable]
public sealed class MoodRemoveEffectEvent : EntityEventArgs
{
    public string EffectId;

    public MoodRemoveEffectEvent(string effectId)
    {
        EffectId = effectId;
    }
}

[ByRefEvent]
public record struct OnSetMoodEvent(EntityUid Receiver, float MoodChangedAmount, bool Cancelled);

[ByRefEvent]
public record struct OnMoodEffect(EntityUid Receiver, string EffectId, float EffectModifier = 1, float EffectOffset = 0);