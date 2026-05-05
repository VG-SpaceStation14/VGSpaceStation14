using Content.Shared._VG.Mood;
using Robust.Shared.GameStates;

namespace Content.Shared._VG.Wagging;

[RegisterComponent]
public sealed partial class AutoWaggingComponent : Component
{
    /// <summary>
    /// Минимальный порог настроения для включения вегинга
    /// </summary>
    [DataField]
    public MoodThreshold RequiredMood = MoodThreshold.Good;

    /// <summary>
    /// Выключать ли вегинг при падении настроения
    /// </summary>
    [DataField]
    public bool DisableWhenBelow = true;

    /// <summary>
    /// Включён ли автоматический вегинг (игрок может отключить через действие)
    /// </summary>
    [DataField]
    public bool AutoWaggingEnabled = true;

    public TimeSpan NextCheckTime;
}