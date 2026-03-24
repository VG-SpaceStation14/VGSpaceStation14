using Robust.Shared.GameStates;

namespace Content.Server._VG.SimpleSkills;

[RegisterComponent]
public sealed partial class WiresPanelSkillRequiredComponent : Component
{
    [DataField(required: true)]
    public string RequiredSkill = string.Empty;
}