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
}

[Prototype("simpleSkill")]
public sealed class SimpleSkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon;
}

/// <summary>
///     Прототип группы навыков
/// </summary>
[Prototype("simpleSkillGroup")]
public sealed class SimpleSkillGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public List<string> Skills = new();
}

[Serializable, NetSerializable]
public struct SimpleSkillInfo
{
    public string ID;
    public string Name;
    public string Description;
    public bool Known;
}