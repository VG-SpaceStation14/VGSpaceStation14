using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Content.Shared.ADT.Droppods.EntitySystems;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropClearCommand : IConsoleCommand
{
    public string Command => "drop_clear";
    public string Description => Loc.GetString("cmd-eventdropclear-desc");
    public string Help => "drop_clear";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        if (entMan.TryGetComponent<EventDropComponent>(player.AttachedEntity.Value, out var comp))
        {
            comp.PreparedItems.Clear();
            shell.WriteLine("Очередь очищена.");
        }
    }
}