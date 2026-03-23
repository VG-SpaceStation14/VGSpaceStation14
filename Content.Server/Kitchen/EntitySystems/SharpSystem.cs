using Content.Server.Body.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Kitchen.EntitySystems;

public sealed class SharpSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly SharedDestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpComponent, AfterInteractEvent>(OnAfterInteract, before: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<SharpComponent, SharpDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<ButcherableComponent, GetVerbsEvent<InteractionVerb>>(OnGetInteractionVerbs);
    }

    private void OnAfterInteract(EntityUid uid, SharpComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is null || !args.CanReach)
            return;

        if (TryStartButcherDoafter(uid, args.Target.Value, args.User))
            args.Handled = true;
    }

    private bool TryStartButcherDoafter(EntityUid knife, EntityUid target, EntityUid user)
    {
        if (!TryComp<ButcherableComponent>(target, out var butcher))
            return false;

        if (!TryComp<SharpComponent>(knife, out var sharp))
            return false;

        if (TryComp<MobStateComponent>(target, out var mobState) && !_mobStateSystem.IsDead(target, mobState))
            return false;

        bool isClaws = knife == user;
        if (butcher.Type != ButcheringType.Knife && !isClaws)
        {
            _popupSystem.PopupEntity(Loc.GetString("butcherable-different-tool", ("target", target)), knife, user);
            return false;
        }

        if (!sharp.Butchering.Add(target))
            return false;

        var needHand = user != knife && !isClaws;

        var doAfter =
            new DoAfterArgs(EntityManager, user, sharp.ButcherDelayModifier * butcher.ButcherDelay, new SharpDoAfterEvent(), knife, target: target, used: knife)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = needHand,
            };
        _doAfterSystem.TryStartDoAfter(doAfter);
        return true;
    }

    private void OnDoAfter(EntityUid uid, SharpComponent component, DoAfterEvent args)
    {
        if (args.Handled || !TryComp<ButcherableComponent>(args.Args.Target, out var butcher))
            return;

        if (args.Cancelled)
        {
            component.Butchering.Remove(args.Args.Target.Value);
            return;
        }

        component.Butchering.Remove(args.Args.Target.Value);

        var spawnEntities = EntitySpawnCollection.GetSpawns(butcher.SpawnedEntities, _robustRandom);
        var coords = _transform.GetMapCoordinates(args.Args.Target.Value);
        EntityUid popupEnt = default!;

        if (_containerSystem.TryGetContainingContainer(args.Args.Target.Value, out var container))
        {
            foreach (var proto in spawnEntities)
            {
                popupEnt = SpawnInContainerOrDrop(proto, container.Owner, container.ID);
            }
        }
        else
        {
            foreach (var proto in spawnEntities)
            {
                popupEnt = Spawn(proto, coords.Offset(_robustRandom.NextVector2(0.25f)));
            }
        }

        var isClaws = uid == args.Args.User;
        var popupType = HasComp<MobStateComponent>(args.Args.Target.Value)
            ? PopupType.LargeCaution
            : PopupType.Small;

        var successMessage = isClaws
            ? Loc.GetString("butcherable-claws-butchered-success", ("target", args.Args.Target.Value))
            : Loc.GetString("butcherable-knife-butchered-success",
                ("target", args.Args.Target.Value),
                ("knife", Identity.Entity(uid, EntityManager)));

        _popupSystem.PopupEntity(successMessage,
            popupEnt,
            args.Args.User,
            popupType);

        _bodySystem.GibBody(args.Args.Target.Value);
        _destructibleSystem.DestroyEntity(args.Args.Target.Value);

        args.Handled = true;

        _adminLogger.Add(LogType.Gib,
            $"{ToPrettyString(args.User):user} " +
            $"has butchered {ToPrettyString(args.Target):target} " +
            $"with {ToPrettyString(args.Used):knife}");
    }

    private void OnGetInteractionVerbs(EntityUid uid, ButcherableComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (component.Type != ButcheringType.Knife || !args.CanAccess || !args.CanInteract)
            return;

        bool hasSharp = TryComp<SharpComponent>(args.User, out var userSharpComp);
        if (!hasSharp && args.Hands == null)
            return;

        var disabled = false;
        string? message = null;

        bool hasUsingSharp = TryComp<SharpComponent>(args.Using, out var usingSharpComp);
        if (!hasUsingSharp && !hasSharp)
        {
            disabled = true;
            message = Loc.GetString("butcherable-need-knife-or-claws",
                ("target", uid));
        }
        else if (_containerSystem.IsEntityInContainer(uid))
        {
            disabled = true;
            message = Loc.GetString("butcherable-not-in-container",
                ("target", uid));
        }
        else if (TryComp<MobStateComponent>(uid, out var state) && !_mobStateSystem.IsDead(uid, state))
        {
            disabled = true;
            message = Loc.GetString("butcherable-mob-isnt-dead");
        }

        EntityUid sharpObject = default;
        if (usingSharpComp != null)
            sharpObject = args.Using!.Value;
        else if (hasSharp)
            sharpObject = args.User;

        InteractionVerb verb = new()
        {
            Act = () =>
            {
                if (!disabled && sharpObject != default)
                    TryStartButcherDoafter(sharpObject, args.Target, args.User);
            },
            Message = message,
            Disabled = disabled,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
            Text = Loc.GetString("butcherable-verb-name"),
        };

        args.Verbs.Add(verb);
    }
}