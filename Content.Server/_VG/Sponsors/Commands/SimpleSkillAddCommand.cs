using Content.Server.Administration;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.SimpleSkills;

[AdminCommand(AdminFlags.Admin)]
public sealed class SimpleSkillAddCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "simpleskilladd";
    public override string Description => Loc.GetString("cmd-simpleskilladd-desc");
    public override string Help => Loc.GetString("cmd-simpleskilladd-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var player = shell.Player?.AttachedEntity;
        
        if (player == null)
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskilladd-no-entity"));
            return;
        }

        var skillId = args[0];
        
        if (!_prototypeManager.HasIndex<SimpleSkillPrototype>(skillId))
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskilladd-invalid-skill", ("skill", skillId)));
            return;
        }

        var skillSystem = EntityManager.System<SimpleSkillSystem>();
        skillSystem.AddSkill(player.Value, skillId);
        
        shell.WriteLine(Loc.GetString("cmd-simpleskilladd-success", ("skill", skillId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var skills = _prototypeManager.EnumeratePrototypes<SimpleSkillPrototype>()
                .Select(x => x.ID)
                .ToList();
            return CompletionResult.FromHintOptions(skills, Loc.GetString("cmd-simpleskilladd-hint"));
        }

        return CompletionResult.Empty;
    }
}