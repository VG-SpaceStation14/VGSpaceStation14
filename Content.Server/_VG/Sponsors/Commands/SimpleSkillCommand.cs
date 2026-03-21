using Content.Server.Administration;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.SimpleSkills;

[AdminCommand(AdminFlags.Admin)]
public sealed class SimpleSkillListCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "simpleskilllist";
    public override string Description => Loc.GetString("cmd-simpleskilllist-desc");
    public override string Help => Loc.GetString("cmd-simpleskilllist-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.WriteLine(Loc.GetString("cmd-simpleskilllist-header"));
        
        foreach (var proto in _prototypeManager.EnumeratePrototypes<SimpleSkillPrototype>())
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskilllist-line",
                ("id", proto.ID),
                ("name", proto.Name)));
        }
    }
}