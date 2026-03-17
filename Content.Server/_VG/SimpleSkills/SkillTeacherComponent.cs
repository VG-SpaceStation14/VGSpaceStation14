namespace Content.Server._VG.SimpleSkills;

[RegisterComponent]
public sealed partial class SkillTeacherComponent : Component
{
    /// <summary>
    ///     Навык, которому может обучать этот игрок
    /// </summary>
    [DataField(required: true)]
    public string SkillId = string.Empty;

    /// <summary>
    ///     Студент, которого сейчас обучаем
    /// </summary>
    public EntityUid? Student;

    /// <summary>
    ///     Книга, используемая для обучения (должна быть в руках)
    /// </summary>
    public EntityUid? Book;

    /// <summary>
    ///     ID DoAfter для обучения (индекс)
    /// </summary>
    public uint? DoAfterId;
}