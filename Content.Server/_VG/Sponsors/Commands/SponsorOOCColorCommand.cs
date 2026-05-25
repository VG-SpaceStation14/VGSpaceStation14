using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server._VG.Sponsors.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorOOCColorCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorooccolor";
    public override string Description => Loc.GetString("cmd-sponsorooccolor-desc");
    public override string Help => Loc.GetString("cmd-sponsorooccolor-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var username = args[0];
        var color = args[1];

        // Basic hex validation
        if (!color.StartsWith('#') || (color.Length != 4 && color.Length != 7 && color.Length != 9))
        {
            shell.WriteLine(Loc.GetString("cmd-sponsorooccolor-invalid-color", ("color", color)));
            return;
        }

        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);

        if (session != null)
        {
            sponsorsManager.ChangeSponsorColor(session.UserId, color);
            shell.WriteLine(Loc.GetString("cmd-sponsorooccolor-success", 
                ("username", username), 
                ("color", color)));
        }
        else
        {
            var action = new ChangeSponsorColorAction
            {
                Username = username,
                NewColor = color
            };
            sponsorsManager.QueuePendingAction(action);
            shell.WriteLine(Loc.GetString("cmd-sponsorooccolor-queued", ("username", username)));
        }
    }
}