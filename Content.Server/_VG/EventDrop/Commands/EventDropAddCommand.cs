using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropAddCommand : IConsoleCommand
{
    public string Command => "drop_add";
    public string Description => Loc.GetString("cmd-eventdropadd-desc");
    public string Help => Loc.GetString("cmd-eventdropadd-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-eventdropadd-error-id"));
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;
        var comp = entMan.EnsureComponent<EventDropComponent>(actor);
        var presetManager = IoCManager.Resolve<IEventDropPresetManager>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        var arg = args[0];
        
        if (arg.StartsWith("preset:", StringComparison.OrdinalIgnoreCase))
        {
            var presetId = arg.Substring(7);
            
            if (!presetManager.TryGetPreset(presetId, out var preset))
            {
                shell.WriteError(Loc.GetString("cmd-eventdropadd-error-preset", ("preset", presetId)));
                return;
            }
            
            var items = presetManager.GetItemsFromPreset(presetId);
            comp.PreparedItems.AddRange(items);
            
            shell.WriteLine(Loc.GetString("cmd-eventdropadd-success-preset",
                ("preset", presetId),
                ("description", preset?.Description ?? Loc.GetString("eventdrop-preset-default-description")),
                ("count", items.Count),
                ("total", comp.PreparedItems.Count)));
            comp.CurrentPresetName = presetId;
            return;
        }
        
        var protoId = arg;
        if (!protoManager.HasIndex<EntityPrototype>(protoId))
        {
            shell.WriteError(Loc.GetString("cmd-eventdropadd-error-prototype", ("prototype", protoId)));
            return;
        }

        if (!int.TryParse(args.Length > 1 ? args[1] : "1", out var amount) || amount < 1)
        {
            shell.WriteError(Loc.GetString("cmd-eventdropadd-error-amount"));
            amount = 1;
        }

        for (var i = 0; i < amount; i++)
        {
            comp.PreparedItems.Add(new EntProtoId(protoId));
        }

        shell.WriteLine(Loc.GetString("cmd-eventdropadd-success-item",
            ("amount", amount),
            ("prototype", protoId),
            ("total", comp.PreparedItems.Count)));
    }
}