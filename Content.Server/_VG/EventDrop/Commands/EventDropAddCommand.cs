using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Content.Shared.ADT.Droppods.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropAddCommand : IConsoleCommand
{
    public string Command => "drop_add";
    public string Description => Loc.GetString("cmd-eventdropadd-desc");
    public string Help => "drop_add <prototype_id> [amount]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        if (args.Length < 1)
        {
            shell.WriteError("Укажите ID прототипа.");
            return;
        }

        var protoId = args[0];
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        if (!protoManager.HasIndex<EntityPrototype>(protoId))
        {
            shell.WriteError($"Прототип {protoId} не найден в базе!");
            return;
        }

        if (!int.TryParse(args.Length > 1 ? args[1] : "1", out var amount) || amount < 1)
            amount = 1;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;
        
        var comp = entMan.EnsureComponent<EventDropComponent>(actor);

        for (var i = 0; i < amount; i++)
        {
            comp.PreparedItems.Add(new EntProtoId(protoId));
        }

        shell.WriteLine($"Добавлено {amount}x {protoId}. Всего предметов в очереди: {comp.PreparedItems.Count}");
    }
}
