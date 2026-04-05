using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Preferences.Loadouts;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.Sponsors.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorAddLoadoutCommand : LocalizedEntityCommands
{
    public override string Command => "sponsoraddloadout";
    public override string Description => "Adds a custom loadout to a sponsor";
    public override string Help => "sponsoraddloadout <username> <loadoutId>";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine("Usage: sponsoraddloadout <username> <loadoutId>");
            return;
        }

        var username = args[0];
        var loadoutId = args[1];
        
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        
        // Check if loadout prototype exists
        if (!prototypeManager.HasIndex<LoadoutPrototype>(loadoutId))
        {
            shell.WriteLine($"Loadout prototype '{loadoutId}' not found!");
            return;
        }

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        if (session == null)
        {
            shell.WriteLine($"User '{username}' not found!");
            return;
        }

        sponsorsManager.AddCustomLoadout(session.UserId, loadoutId);
        shell.WriteLine($"Added loadout '{loadoutId}' to {username}");
    }
}