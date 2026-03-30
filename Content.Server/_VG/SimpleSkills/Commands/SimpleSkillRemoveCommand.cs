using System.Linq;
using Content.Server.Administration;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.SimpleSkills;

[AdminCommand(AdminFlags.Admin)]
public sealed class SimpleSkillRemoveCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "simpleskillremove";
    public override string Description => Loc.GetString("cmd-simpleskillremove-desc");
    public override string Help => Loc.GetString("cmd-simpleskillremove-help");

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
            shell.WriteLine(Loc.GetString("cmd-simpleskillremove-no-entity"));
            return;
        }

        var skillId = args[0];

        if (!EntityManager.TryGetComponent<SimpleSkillComponent>(player, out var skills))
        {
            shell.WriteLine(Loc.GetString("cmd-simpleskillremove-no-skills"));
            return;
        }

        if (!skills.Skills.ContainsKey(skillId))
        {
            var skillName = _prototypeManager.TryIndex<SimpleSkillPrototype>(skillId, out var proto)
                ? proto.Name
                : skillId;
            shell.WriteLine(Loc.GetString("cmd-simpleskillremove-not-known",
                ("skill", skillName)));
            return;
        }

        skills.Skills.Remove(skillId);
        EntityManager.Dirty(player.Value, skills);
        
        shell.WriteLine(Loc.GetString("cmd-simpleskillremove-success", ("skill", skillId)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var player = shell.Player?.AttachedEntity;
            if (player != null && EntityManager.TryGetComponent<SimpleSkillComponent>(player, out var skills))
            {
                var knownSkills = skills.Skills.Keys.ToList();
                return CompletionResult.FromHintOptions(knownSkills, Loc.GetString("cmd-simpleskillremove-hint"));
            }
        }

        return CompletionResult.Empty;
    }
}