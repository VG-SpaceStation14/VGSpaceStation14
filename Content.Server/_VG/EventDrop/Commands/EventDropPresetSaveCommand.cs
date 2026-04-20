using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropPresetSaveCommand : IConsoleCommand
{
    public string Command => "drop_preset_save";
    public string Description => Loc.GetString("cmd-eventdrop-preset-save-desc");
    public string Help => Loc.GetString("cmd-eventdrop-preset-save-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-save-error-id"));
            return;
        }

        var presetId = args[0];
        var description = args.Length > 1 ? args[1] : string.Empty;
        
        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;
        
        if (!entMan.TryGetComponent<EventDropComponent>(actor, out var comp) || comp.PreparedItems.Count == 0)
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-save-error-empty"));
            return;
        }
        
        var itemGroups = new Dictionary<string, int>();
        foreach (var item in comp.PreparedItems)
        {
            var protoId = item.Id;
            if (itemGroups.ContainsKey(protoId))
                itemGroups[protoId]++;
            else
                itemGroups[protoId] = 1;
        }
        
        var preset = new EventDropPreset
        {
            Name = presetId,
            Description = description,
            Items = itemGroups.Select(x => new PresetItem { PrototypeId = x.Key, Amount = x.Value }).ToList(),
            CreatedBy = player.Name ?? "Unknown",
            CreatedAt = DateTime.UtcNow
        };
        
        var presetManager = IoCManager.Resolve<IEventDropPresetManager>();
        if (presetManager.SavePreset(presetId, preset))
        {
            shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-save-success",
                ("preset", presetId),
                ("total", comp.PreparedItems.Count),
                ("unique", itemGroups.Count)));
                
            if (!string.IsNullOrEmpty(description))
                shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-save-success-desc", ("description", description)));
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-save-error-failed", ("preset", presetId)));
        }
    }
}