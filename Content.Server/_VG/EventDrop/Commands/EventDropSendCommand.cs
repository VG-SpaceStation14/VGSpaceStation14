using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Content.Shared.ADT.Droppods.EntitySystems;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropSendCommand : IConsoleCommand
{
    public string Command => "drop_send";
    public string Description => Loc.GetString("cmd-eventdropsend-desc");
    public string Help => Loc.GetString("cmd-eventdropsend-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;

        if (!entMan.TryGetComponent<EventDropComponent>(actor, out var comp) || comp.PreparedItems.Count == 0)
        {
            shell.WriteError(Loc.GetString("cmd-eventdropsend-error-empty"));
            return;
        }

        if (!entMan.TryGetComponent<TransformComponent>(actor, out var xform))
            return;

        var droppodSys = entMan.System<DroppodSystem>();
        
        droppodSys.CreateDroppod(xform.Coordinates, comp.PreparedItems);

        shell.WriteLine(Loc.GetString("cmd-eventdropsend-success",
            ("coordinates", xform.Coordinates.ToString()),
            ("count", comp.PreparedItems.Count)));
        
        comp.PreparedItems.Clear();
    }
}