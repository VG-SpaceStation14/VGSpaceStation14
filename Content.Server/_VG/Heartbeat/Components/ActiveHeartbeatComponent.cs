using Robust.Shared.GameStates;

namespace Content.Server._VG.Heartbeat.Components;

/// <summary>
/// Runtime component added when the entity is in critical condition;
/// controls heartbeat sound pitch, cooldown, and next playback time.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveHeartbeatComponent : Component
{
    /// <summary>
    /// Sound pitch, calculated from current damage.
    /// </summary>
    [DataField]
    public float Pitch = 1f;

    /// <summary>
    /// Time between heartbeat sounds.
    /// </summary>
    [DataField]
    public TimeSpan NextHeartbeatCooldown = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Absolute game time when the next heartbeat should play.
    /// </summary>
    [DataField]
    public TimeSpan NextHeartbeatTime;
}