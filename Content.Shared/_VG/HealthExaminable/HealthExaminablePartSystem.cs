using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.HealthExaminable;
using Robust.Shared.Utility;

namespace Content.Shared._VG.HealthExaminable;

public sealed class HealthExaminablePartSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealthExaminablePartComponent, HealthBeingExaminedEvent>(OnHealthExamined);
    }

    private void OnHealthExamined(EntityUid uid, HealthExaminablePartComponent comp, HealthBeingExaminedEvent args)
    {
        if (!TryComp<BodyComponent>(uid, out var body))
            return;

        var msg = args.Message;

        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("health-examinable-parts-header"));

        var parts = _bodySystem.GetBodyChildren(uid, body).ToList();
        if (parts.Count == 0)
            return;

        var orderedParts = parts
            .OrderBy(p => GetPartOrder(p.Component.PartType, p.Component.Symmetry))
            .ToList();

        foreach (var part in orderedParts)
        {
            var partEntity = part.Id;
            var partComp = part.Component;

            var status = GetPartDamageStatus(partEntity);
            var partName = GetPartLocalizedName(partComp.PartType, partComp.Symmetry);

            msg.PushNewline();
            msg.AddMarkupOrThrow($"- {partName}: {status}");
        }
    }

    private int GetPartOrder(BodyPartType type, BodyPartSymmetry symmetry)
    {
        return type switch
        {
            BodyPartType.Head => 1,
            BodyPartType.Torso => 2,
            BodyPartType.Arm when symmetry == BodyPartSymmetry.Left => 3,
            BodyPartType.Arm when symmetry == BodyPartSymmetry.Right => 4,
            BodyPartType.Leg when symmetry == BodyPartSymmetry.Left => 5,
            BodyPartType.Leg when symmetry == BodyPartSymmetry.Right => 6,
            BodyPartType.Hand when symmetry == BodyPartSymmetry.Left => 7,
            BodyPartType.Hand when symmetry == BodyPartSymmetry.Right => 8,
            BodyPartType.Foot when symmetry == BodyPartSymmetry.Left => 9,
            BodyPartType.Foot when symmetry == BodyPartSymmetry.Right => 10,
            _ => 99
        };
    }

    private string GetPartLocalizedName(BodyPartType type, BodyPartSymmetry symmetry)
    {
        var key = symmetry switch
        {
            BodyPartSymmetry.Left => $"health-examinable-part-{type.ToString().ToLower()}-left",
            BodyPartSymmetry.Right => $"health-examinable-part-{type.ToString().ToLower()}-right",
            _ => $"health-examinable-part-{type.ToString().ToLower()}"
        };
        return Loc.GetString(key);
    }

    private string GetPartDamageStatus(EntityUid partEntity)
    {
        if (!TryComp<DamageableComponent>(partEntity, out var damageable))
            return Loc.GetString("health-examinable-part-ok");

        var totalDamage = damageable.TotalDamage;
        if (totalDamage == FixedPoint2.Zero)
            return Loc.GetString("health-examinable-part-ok");

        if (totalDamage < 15)
            return Loc.GetString("health-examinable-wound-light");
        if (totalDamage < 30)
            return Loc.GetString("health-examinable-wound-moderate");
        if (totalDamage < 50)
            return Loc.GetString("health-examinable-wound-severe");

        return Loc.GetString("health-examinable-wound-critical");
    }
}