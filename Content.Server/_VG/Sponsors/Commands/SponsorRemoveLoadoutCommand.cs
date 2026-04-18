using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._VG.Sponsors.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorRemoveLoadoutCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorremoveloadout";
    public override string Description => Loc.GetString("cmd-sponsorremoveloadout-desc");
    public override string Help => Loc.GetString("cmd-sponsorremoveloadout-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var username = args[0];
        var loadoutId = args[1];
        
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        
        if (session != null)
        {
            sponsorsManager.RemoveCustomLoadout(session.UserId, loadoutId);
            shell.WriteLine(Loc.GetString("cmd-sponsorremoveloadout-success",
                ("loadoutId", loadoutId),
                ("username", username)));
        }
        else
        {
            var action = new RemoveLoadoutAction
            {
                Username = username,
                LoadoutId = loadoutId
            };
            sponsorsManager.QueuePendingAction(action);
            shell.WriteLine(Loc.GetString("cmd-sponsorremoveloadout-queued", ("username", username)));
        }
    }
}