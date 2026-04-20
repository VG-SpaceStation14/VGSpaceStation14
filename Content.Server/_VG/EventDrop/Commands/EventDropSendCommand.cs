using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Content.Shared.ADT.Droppods.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropSendCommand : IConsoleCommand
{
    public string Command => "drop_send";
    public string Description => Loc.GetString("cmd-eventdropsend-desc");
    public string Help => "drop_send";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;

        if (!entMan.TryGetComponent<EventDropComponent>(actor, out var comp) || comp.PreparedItems.Count == 0)
        {
            shell.WriteError("Очередь пуста! Сначала используйте drop_add.");
            return;
        }

        if (!entMan.TryGetComponent<TransformComponent>(actor, out var xform))
            return;

        var droppodSys = entMan.System<DroppodSystem>();
        
        droppodSys.CreateDroppod(xform.Coordinates, comp.PreparedItems);

        shell.WriteLine($"Капсула отправлена на координаты: {xform.Coordinates}. Предметов: {comp.PreparedItems.Count}");
        
        comp.PreparedItems.Clear();
    }
}