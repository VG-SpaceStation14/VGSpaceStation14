using System.Linq;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Robust.Shared.Utility;

namespace Content.Shared.HealthExaminable;

public sealed class HealthExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HealthExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(EntityUid uid, HealthExaminableComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damage))
            return;

        var detailsRange = _examineSystem.IsInDetailsRange(args.User, uid);

        var verb = new ExamineVerb()
        {
            Act = () =>
            {
                var markup = CreateMarkup(uid, component, damage);
                _examineSystem.SendExamineTooltip(args.User, uid, markup, false, false);
            },
            Text = Loc.GetString("health-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("health-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/rejuvenate.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    public FormattedMessage CreateMarkup(EntityUid uid, HealthExaminableComponent component, DamageableComponent damage)
    {
        var msg = new FormattedMessage();

        // ---- Существующая логика урона (не трогаем) ----
        var first = true;
        foreach (var type in component.ExaminableTypes)
        {
            if (!damage.Damage.DamageDict.TryGetValue(type, out var dmg))
                continue;

            if (dmg == FixedPoint2.Zero)
                continue;

            FixedPoint2 closest = FixedPoint2.Zero;
            string chosenLocStr = string.Empty;
            foreach (var threshold in component.Thresholds)
            {
                var str = $"health-examinable-{component.LocPrefix}-{type}-{threshold}";
                var tempLocStr = Loc.GetString($"health-examinable-{component.LocPrefix}-{type}-{threshold}", ("target", Identity.Entity(uid, EntityManager)));

                if (tempLocStr == str)
                    continue;

                if (dmg > threshold && threshold > closest)
                {
                    chosenLocStr = tempLocStr;
                    closest = threshold;
                }
            }

            if (closest == FixedPoint2.Zero)
                continue;

            if (!first)
                msg.PushNewline();
            else
                first = false;

            msg.AddMarkupOrThrow(chosenLocStr);
        }

        if (msg.IsEmpty)
        {
            msg.AddMarkupOrThrow(Loc.GetString($"health-examinable-{component.LocPrefix}-none"));
        }

        // ---- СОСТОЯНИЕ ЧАСТЕЙ ТЕЛА (на основе DamageableComponent) ----
        msg.PushNewline();
        msg.AddMarkupOrThrow(Loc.GetString("health-examinable-parts-header"));

        // Получаем все части тела
        var parts = _bodySystem.GetBodyChildren(uid).ToList();

        // Сортируем в нужном порядке
        var orderedParts = parts.OrderBy(p => GetPartOrder(p.Component.PartType, p.Component.Symmetry)).ToList();

        foreach (var part in orderedParts)
        {
            var partEntity = part.Id;
            var partComp = part.Component;

            var status = GetPartDamageStatus(partEntity);
            var partName = GetPartLocalizedName(partComp.PartType, partComp.Symmetry);

            msg.PushNewline();
            msg.AddMarkupOrThrow($"- {partName}: {status}");
        }
        // ------------------------------------------------------------

        RaiseLocalEvent(uid, new HealthBeingExaminedEvent(msg), true);

        return msg;
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

        // Пороги: лёгкие, средние, тяжёлые, критические раны (в процентах от максимального здоровья)
        // Если у части нет максимального здоровья, используем абсолютные значения
        var maxHealth = damageable.TotalDamage; // не совсем правильно, но для простоты
        // Лучше использовать damageable.DamagePerGroup? Но проще по сумме урона: чем больше урон, тем серьёзнее
        if (totalDamage < 15)
            return Loc.GetString("health-examinable-wound-light");
        if (totalDamage < 30)
            return Loc.GetString("health-examinable-wound-moderate");
        if (totalDamage < 50)
            return Loc.GetString("health-examinable-wound-severe");
        return Loc.GetString("health-examinable-wound-critical");
    }
}

/// <summary>
///     A class raised on an entity whose health is being examined
///     in order to add special text that is not handled by the
///     damage thresholds.
/// </summary>
public sealed class HealthBeingExaminedEvent
{
    public FormattedMessage Message;

    public HealthBeingExaminedEvent(FormattedMessage message)
    {
        Message = message;
    }
}