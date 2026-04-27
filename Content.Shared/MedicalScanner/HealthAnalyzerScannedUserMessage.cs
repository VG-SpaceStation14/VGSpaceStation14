using Content.Shared._VG.Targeting;
using Content.Shared.FixedPoint; // ADT-Tweak
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public List<(string ReagentId, FixedPoint2 Quantity)>? MetabolizingReagents; // ADT-Tweak
    public Dictionary<TargetBodyPart, TargetIntegrity>? Body; // VG: surgery - статус всех частей тела
    public NetEntity? Part; // VG: surgery - выбранная часть тела

    public HealthAnalyzerScannedUserMessage(
        NetEntity? targetEntity,
        float temperature,
        float bloodLevel,
        bool? scanMode,
        bool? bleeding,
        bool? unrevivable,
        List<(string ReagentId, FixedPoint2 Quantity)>? metabolizingReagents = null,
        Dictionary<TargetBodyPart, TargetIntegrity>? body = null,
        NetEntity? part = null)
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        MetabolizingReagents = metabolizingReagents; // ADT-Tweak
        Body = body; // VG: surgery
        Part = part; // VG: surgery
    }
}

// start-_VG: surgery
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(NetEntity? owner, TargetBodyPart? bodyPart) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Owner = owner;
    public readonly TargetBodyPart? BodyPart = bodyPart;
}
// end-_VG: surgery