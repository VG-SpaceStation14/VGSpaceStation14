using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._VG.Sponsors;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorAddCommand : LocalizedEntityCommands
{
    public override string Command => "sponsoradd";
    public override string Description => Loc.GetString("cmd-sponsoradd-desc");
    public override string Help => Loc.GetString("cmd-sponsoradd-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var username = args[0];
        
        if (!int.TryParse(args[1], out var tier) || tier < 1 || tier > 3)
        {
            shell.WriteLine(Loc.GetString("cmd-sponsoradd-invalid-tier"));
            return;
        }

        DateTime? expireDate = null;
        if (args.Length >= 3 && int.TryParse(args[2], out var days))
        {
            expireDate = DateTime.UtcNow.AddDays(days);
        }

        var notes = args.Length >= 4 ? args[3] : null;

        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        
        if (session != null)
        {
            sponsorsManager.AddSponsor(session.UserId, username, tier, expireDate, notes);
            shell.WriteLine(Loc.GetString("cmd-sponsoradd-success",
                ("username", username),
                ("tier", tier)));
        }
        else
        {
            var action = new AddSponsorAction
            {
                Username = username,
                Tier = tier,
                ExpireDate = expireDate,
                Notes = notes
            };
            sponsorsManager.QueuePendingAction(action);
            shell.WriteLine(Loc.GetString("cmd-sponsoradd-queued", ("username", username)));
        }
    }
}