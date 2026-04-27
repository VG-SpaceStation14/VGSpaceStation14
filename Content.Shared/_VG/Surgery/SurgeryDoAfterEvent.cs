using Content.Shared.DoAfter;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._VG.Surgery;

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;
    public readonly NetEntity Part;

    public SurgeryDoAfterEvent(EntProtoId surgery, EntProtoId step, NetEntity part)
    {
        Surgery = surgery;
        Step = step;
        Part = part;
    }

    public override DoAfterEvent Clone() => new SurgeryDoAfterEvent(Surgery, Step, Part);
}
