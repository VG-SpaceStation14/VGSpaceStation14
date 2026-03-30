using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._VG.Sponsors;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorRemoveCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorremove";
    public override string Description => Loc.GetString("cmd-sponsorremove-desc");
    public override string Help => Loc.GetString("cmd-sponsorremove-help");

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
        if (session == null)
        {
            shell.WriteLine(Loc.GetString("cmd-sponsorremove-user-not-found", ("username", username)));
            return;
        }

        sponsorsManager.RemoveSponsor(session.UserId);
        shell.WriteLine(Loc.GetString("cmd-sponsorremove-success", ("username", username)));
    }
}