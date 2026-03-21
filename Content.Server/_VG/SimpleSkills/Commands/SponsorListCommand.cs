using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._VG.Sponsors;

[AdminCommand(AdminFlags.Admin)]
public sealed class SponsorListCommand : LocalizedEntityCommands
{
    public override string Command => "sponsorlist";
    public override string Description => Loc.GetString("cmd-sponsorlist-desc");
    public override string Help => Loc.GetString("cmd-sponsorlist-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sponsorsManager = IoCManager.Resolve<SponsorsManager>();
        var sponsors = sponsorsManager.ListSponsors();

        if (sponsors.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-sponsorlist-empty"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-sponsorlist-header"));
        foreach (var sponsor in sponsors)
        {
            var expireStr = sponsor.ExpireDate?.ToString("yyyy-MM-dd") ?? Loc.GetString("cmd-sponsorlist-never");
            shell.WriteLine(Loc.GetString("cmd-sponsorlist-line",
                ("username", sponsor.Username),
                ("tier", sponsor.Tier),
                ("expire", expireStr)));
            
            if (!string.IsNullOrEmpty(sponsor.Notes))
            {
                shell.WriteLine(Loc.GetString("cmd-sponsorlist-notes", ("notes", sponsor.Notes)));
            }
        }
    }
}