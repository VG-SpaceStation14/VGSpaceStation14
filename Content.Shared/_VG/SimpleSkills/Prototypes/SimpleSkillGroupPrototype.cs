using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._VG.SimpleSkills;

/// <summary>
///     Прототип группы навыков
/// </summary>
[Prototype("simpleSkillGroup")]
public sealed partial class SimpleSkillGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [DataField("skills")]
    public List<string> Skills { get; private set; } = new();

    /// <summary>
    ///     Запрещает выдачу всех навыков (fallback) для этой группы, даже если она пустая
    /// </summary>
    [DataField("preventFallback")]
    public bool PreventFallback { get; private set; } = false;
}