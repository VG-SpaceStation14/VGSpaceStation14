using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._VG.SimpleSkills;

[Serializable, NetSerializable]
public struct SimpleSkillInfo
{
    public string ID;
    public string Name;
    public string Description;
    public bool Known;
}