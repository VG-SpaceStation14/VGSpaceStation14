using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.Sponsors;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorAddCommand : IConsoleCommand
{
    public string Command => "sponsoradd";
    public string Description => "Adds or updates a sponsor";
    public string Help => "sponsoradd <username> <tier> [expire_days] [notes]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Help);
            return;
        }

        var username = args[0];
        if (!int.TryParse(args[1], out var tier) || tier < 1 || tier > 3)
        {
            shell.WriteLine("Tier must be 1, 2, or 3");
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

        // Ищем пользователя по имени
        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        if (session == null)
        {
            shell.WriteLine($"User {username} not found online. Use UserId directly?");
            return;
        }

        sponsorsManager.AddSponsor(session.UserId, username, tier, expireDate, notes);
        shell.WriteLine($"Sponsor {username} (Tier {tier}) added/updated");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorRemoveCommand : IConsoleCommand
{
    public string Command => "sponsorremove";
    public string Description => "Removes a sponsor";
    public string Help => "sponsorremove <username>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var username = args[0];
        var playerManager = IoCManager.Resolve<IPlayerManager>();
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        if (session == null)
        {
            shell.WriteLine($"User {username} not found online");
            return;
        }

        sponsorsManager.RemoveSponsor(session.UserId);
        shell.WriteLine($"Sponsor {username} removed");
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorListCommand : IConsoleCommand
{
    public string Command => "sponsorlist";
    public string Description => "Lists all sponsors";
    public string Help => "sponsorlist";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();
        var sponsors = sponsorsManager.ListSponsors();

        if (sponsors.Count == 0)
        {
            shell.WriteLine("No sponsors found");
            return;
        }

        shell.WriteLine("=== Sponsors List ===");
        foreach (var sponsor in sponsors)
        {
            var expireStr = sponsor.ExpireDate?.ToString("yyyy-MM-dd") ?? "never";
            shell.WriteLine($"{sponsor.Username} - Tier {sponsor.Tier} - Expires: {expireStr}");
            if (!string.IsNullOrEmpty(sponsor.Notes))
                shell.WriteLine($"  Notes: {sponsor.Notes}");
        }
    }
}