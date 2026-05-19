using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._VG.Tools;

/// <summary>
/// Компонент для сварочного инструмента, добавляющий циклический звук во время сварки.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeldingSoundComponent : Component
{
    /// <summary>
    /// Звук, который проигрывается во время сварки.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_VG/Items/welder.ogg");

    /// <summary>
    /// Громкость звука (от 0 до 100).
    /// </summary>
    [DataField]
    public float Volume = 5f;

    /// <summary>
    /// Сущность, проигрывающая зацикленный звук (серверный хендл).
    /// </summary>
    public EntityUid? StreamHandle;
}