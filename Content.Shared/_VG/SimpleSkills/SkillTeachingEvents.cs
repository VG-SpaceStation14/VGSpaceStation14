using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._VG.SimpleSkills;

[Serializable, NetSerializable]
public sealed partial class SkillLearnDoAfterEvent : SimpleDoAfterEvent
{
    public string SkillId { get; }

    public SkillLearnDoAfterEvent(string skillId)
    {
        SkillId = skillId;
    }
}

[Serializable, NetSerializable]
public sealed partial class SkillTeachDoAfterEvent : SimpleDoAfterEvent
{
    public string SkillId { get; }
    public NetEntity Student { get; }

    public SkillTeachDoAfterEvent(string skillId, NetEntity student)
    {
        SkillId = skillId;
        Student = student;
    }
}