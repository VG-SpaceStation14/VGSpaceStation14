using Content.Server.Popups;
using Content.Shared._VG.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Input; // ← Добавлено для InputModifiers
using System.Linq;

namespace Content.Server._VG.Storage;

public sealed class SmartOreBagSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SmartOreBagComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SmartOreBagComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SmartOreBagComponent, InteractHandEvent>(OnInteractHand); // ← NEW: ALT+ЛКМ
        SubscribeNetworkEvent<SmartOreBagUpdateMessage>(OnUpdateIgnored);
    }

    private void OnGetVerbs(EntityUid uid, SmartOreBagComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new Verb
        {
            Text = "Настройка руд",
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => OpenConfigWindow(uid, args.User, component),
            Priority = 4
        };
        args.Verbs.Add(verb);
    }

    private void OnInteractUsing(EntityUid uid, SmartOreBagComponent component, InteractUsingEvent args)
    {
        OpenConfigWindow(uid, args.User, component);
        args.Handled = true;
    }

    // ← NEW: Обработчик ALT+ЛКМ
    private void OnInteractHand(EntityUid uid, SmartOreBagComponent component, InteractHandEvent args)
    {
        if (args.Modifiers.HasFlag(InputModifiers.Alt))
        {
            OpenConfigWindow(uid, args.User, component);
            args.Handled = true;
        }
    }

    private void OpenConfigWindow(EntityUid uid, EntityUid user, SmartOreBagComponent component)
    {
        var netEntity = GetNetEntity(uid);
        var msg = new OpenSmartOreBagWindowMessage(netEntity, component.IgnoredOres);
        RaiseNetworkEvent(msg, user);
    }

    private void OnUpdateIgnored(SmartOreBagUpdateMessage msg, EntitySessionEventArgs args)
    {
        var uid = GetEntity(msg.Entity);
        
        if (!TryComp<SmartOreBagComponent>(uid, out var component))
            return;

        component.IgnoredOres = msg.IgnoredOres;
        Dirty(uid, component);
    }
}