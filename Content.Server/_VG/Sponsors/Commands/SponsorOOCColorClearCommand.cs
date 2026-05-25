using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._VG.Sponsors.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorOOCColorClearCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorooccolorclear";
    public override string Description => Loc.GetString("cmd-sponsorooccolorclear-desc");
    public override string Help => Loc.GetString("cmd-sponsorooccolorclear-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var username = args[0];
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);

        if (session != null)
        {
            sponsorsManager.ClearSponsorColor(session.UserId);
            shell.WriteLine(Loc.GetString("cmd-sponsorooccolorclear-success", ("username", username)));
        }
        else
        {
            var action = new ClearSponsorColorAction
            {
                Username = username
            };
            sponsorsManager.QueuePendingAction(action);
            shell.WriteLine(Loc.GetString("cmd-sponsorooccolorclear-queued", ("username", username)));
        }
    }
}