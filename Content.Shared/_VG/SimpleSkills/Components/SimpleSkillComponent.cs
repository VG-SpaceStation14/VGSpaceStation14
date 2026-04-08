using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._VG.SimpleSkills;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SimpleSkillComponent : Component
{
    /// <summary>
    ///     Словарь навыков: ID навыка -> есть/нет (true/false)
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, bool> Skills = new();
    
    /// <summary>
    ///     Группа навыков для профессии
    /// </summary>
    [DataField]
    public string? SkillGroup;

    [DataField]
    public bool FallbackPrevented = false;
}

