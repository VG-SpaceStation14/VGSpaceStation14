using System.Linq;
using Content.Client.Message;
using Content.Shared._VG.SimpleSkills;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._VG.SimpleSkills;

public sealed class SimpleSkillInfoSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <summary>
    ///     Получает список ТОЛЬКО ИЗУЧЕННЫХ навыков для текущего игрока
    /// </summary>
    public List<(SimpleSkillPrototype Proto, bool Known)> GetKnownSkills()
    {
        var result = new List<(SimpleSkillPrototype Proto, bool Known)>();
        
        var player = _player.LocalEntity;
        if (player == null)
            return result;

        if (!TryComp<SimpleSkillComponent>(player, out var skills))
            return result;

        var allSkills = _prototype.EnumeratePrototypes<SimpleSkillPrototype>().ToList();
        
        foreach (var (skillId, known) in skills.Skills)
        {
            if (!known) continue;
            
            var proto = allSkills.FirstOrDefault(p => p.ID == skillId);
            if (proto != null)
            {
                result.Add((proto, true)); 
            }
        }
        
        result.Sort((a, b) => string.Compare(a.Proto.Name, b.Proto.Name, StringComparison.Ordinal));

        return result;
    }

    /// <summary>
    ///     Для отладки - показывает все навыки (и изученные, и нет)
    /// </summary>
    public List<(SimpleSkillPrototype Proto, bool Known)> GetAllSkillsWithStatus()
    {
        var result = new List<(SimpleSkillPrototype Proto, bool Known)>();  
        
        var player = _player.LocalEntity;
        if (player == null)
            return result;

        TryComp<SimpleSkillComponent>(player, out var skills);

        var allSkills = _prototype.EnumeratePrototypes<SimpleSkillPrototype>().ToList();
        allSkills.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        foreach (var proto in allSkills)
        {
            var known = skills?.Skills.TryGetValue(proto.ID, out var isKnown) == true && isKnown;
            result.Add((proto, known));  
        }

        return result;
    }
}