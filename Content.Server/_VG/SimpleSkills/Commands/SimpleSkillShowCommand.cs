using Content.Server.Administration;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.SimpleSkills;

[AdminCommand(AdminFlags.Admin)]
public sealed class SimpleSkillShowCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "simpleskillshow";
    public override string Description => Loc.GetString("cmd-simpleskillshow-desc");
    public override string Help => Loc.GetString("cmd-simpleskillshow-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player?.AttachedEntity;
        
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskillshow-no-entity"));
            return;
        }

        if (!EntityManager.TryGetComponent<SimpleSkillComponent>(player, out var skills))
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskillshow-no-skills"));
            return;
        }

        var hasSkills = false;
        shell.WriteLine(Loc.GetString("cmd-simpleskillshow-header"));
        
        foreach (var (skillId, known) in skills.Skills)
        {
            if (known)
            {
                hasSkills = true;
                var name = _prototypeManager.TryIndex<SimpleSkillPrototype>(skillId, out var proto) 
                    ? proto.Name 
                    : skillId;
                shell.WriteLine(Loc.GetString("cmd-simpleskillshow-line",
                    ("name", name),
                    ("id", skillId)));
            }
        }

        if (!hasSkills)
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskillshow-empty"));
        }
    }
}