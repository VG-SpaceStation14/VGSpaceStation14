using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Preferences.Loadouts;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.Sponsors.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorRemoveLoadoutCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorremoveloadout";
    public override string Description => "Removes a custom loadout from a sponsor";
    public override string Help => "sponsorremoveloadout <username> <loadoutId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine("Usage: sponsorremoveloadout <username> <loadoutId>");
            return;
        }

        var username = args[0];
        var loadoutId = args[1];
        
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        if (session == null)
        {
            shell.WriteLine($"User '{username}' not found!");
            return;
        }

        sponsorsManager.RemoveCustomLoadout(session.UserId, loadoutId);
        shell.WriteLine($"Removed loadout '{loadoutId}' from {username}");
    }
}