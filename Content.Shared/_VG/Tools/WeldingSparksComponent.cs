using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared._VG.Tools;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeldingSparksComponent : Component
{
    /// <summary>
    /// Prototype эффекта, который спавнится при сварке.
    /// </summary>
    [DataField]
    public EntProtoId EffectPrototype = "EffectWeldingSparks";

    /// <summary>
    /// Словарь активных эффектов, связанных с DoAfter.
    /// </summary>
    public Dictionary<DoAfterId, EntityUid> SpawnedEffects = new();
}