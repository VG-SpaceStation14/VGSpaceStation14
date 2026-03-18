using Robust.Shared.Serialization;

namespace Content.Shared._VG.SimpleSkills;

[Serializable, NetSerializable]
public sealed class SkillsChangedEvent : EntityEventArgs
{
    public NetEntity Player { get; }

    public SkillsChangedEvent(NetEntity player)
    {
        Player = player;
    }
}