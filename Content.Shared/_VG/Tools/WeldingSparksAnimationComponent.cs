using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared._VG.Tools;

/// <summary>
/// Добавляется на объект, который может быть заварен, чтобы анимировать искры.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeldingSparksAnimationComponent : Component
{
    /// <summary>
    /// Начальное смещение эффекта (локальные координаты спрайта).
    /// </summary>
    [DataField]
    public Vector2 StartingOffset;

    /// <summary>
    /// Конечное смещение (если null, то -StartingOffset).
    /// </summary>
    [DataField]
    public Vector2? EndingOffset;
}