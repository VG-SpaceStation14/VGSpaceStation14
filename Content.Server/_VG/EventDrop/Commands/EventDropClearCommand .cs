using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropClearCommand : IConsoleCommand
{
    public string Command => "drop_clear";
    public string Description => Loc.GetString("cmd-eventdropclear-desc");
    public string Help => Loc.GetString("cmd-eventdropclear-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        var entMan = IoCManager.Resolve<IEntityManager>();
        if (entMan.TryGetComponent<EventDropComponent>(player.AttachedEntity.Value, out var comp))
        {
            if (comp.PreparedItems.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-eventdropclear-empty"));
                return;
            }
            
            comp.PreparedItems.Clear();
            shell.WriteLine(Loc.GetString("cmd-eventdropclear-success"));
        }
    }
}