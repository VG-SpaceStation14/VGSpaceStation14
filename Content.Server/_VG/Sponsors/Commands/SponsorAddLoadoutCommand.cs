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
    public override string Description => Loc.GetString("cmd-sponsoraddloadout-desc");
    public override string Help => Loc.GetString("cmd-sponsoraddloadout-help");

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
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        
        if (!prototypeManager.HasIndex<LoadoutPrototype>(loadoutId))
        {
            shell.WriteLine(Loc.GetString("cmd-sponsoraddloadout-loadout-not-found", ("loadoutId", loadoutId)));
            return;
        }

        var session = playerManager.Sessions.FirstOrDefault(s => s.Name == username);
        
        if (session != null)
        {
            sponsorsManager.AddCustomLoadout(session.UserId, loadoutId);
            shell.WriteLine(Loc.GetString("cmd-sponsoraddloadout-success", 
                ("loadoutId", loadoutId), 
                ("username", username)));
        }
        else
        {
            var action = new AddLoadoutAction
            {
                Username = username,
                LoadoutId = loadoutId
            };
            sponsorsManager.QueuePendingAction(action);
            shell.WriteLine(Loc.GetString("cmd-sponsoraddloadout-queued", ("username", username)));
        }
    }
}