using Content.Server.Administration;
using Content.Shared._VG.SimpleSkills;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.SimpleSkills;

[AdminCommand(AdminFlags.Admin)]
public sealed class SimpleSkillCommand : IConsoleCommand
{
    public string Command => "simpleSkill";
    public string Description => "Управление простыми навыками";
    public string Help => "simpleSkill list/show/add/remove <skillId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Help);
            return;
        }

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var player = shell.Player?.AttachedEntity;
        
        if (player == null)
        {
            shell.WriteLine("Вы не привязаны к сущности");
            return;
        }

        var skillSystem = entityManager.System<SimpleSkillSystem>();
        var protoManager = IoCManager.Resolve<IPrototypeManager>();

        switch (args[0].ToLower())
        {
            case "list":
                shell.WriteLine("=== Доступные навыки ===");
                foreach (var proto in protoManager.EnumeratePrototypes<SimpleSkillPrototype>())
                {
                    shell.WriteLine($"- {proto.ID}: {proto.Name}");
                }
                break;

            case "show":
                if (!entityManager.TryGetComponent<SimpleSkillComponent>(player, out var skills))
                {
                    shell.WriteLine("У вас нет навыков");
                    return;
                }

                shell.WriteLine("=== Ваши навыки ===");
                foreach (var (skillId, known) in skills.Skills)
                {
                    if (known)
                    {
                        var name = protoManager.TryIndex<SimpleSkillPrototype>(skillId, out var proto) 
                            ? proto.Name 
                            : skillId;
                        shell.WriteLine($"- {name} ({skillId})");
                    }
                }
                break;

            case "add":
                if (args.Length < 2)
                {
                    shell.WriteLine("Укажите ID навыка");
                    return;
                }

                skillSystem.AddSkill(player.Value, args[1]);
                shell.WriteLine($"Навык {args[1]} добавлен");
                break;
                
            case "remove":
                if (args.Length < 2)
                {
                    shell.WriteLine("Укажите ID навыка");
                    return;
                }
                
                if (entityManager.TryGetComponent<SimpleSkillComponent>(player, out var removeSkills))
                {
                    removeSkills.Skills.Remove(args[1]);
                    shell.WriteLine($"Навык {args[1]} удален");
                }
                break;
        }
    }
}