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