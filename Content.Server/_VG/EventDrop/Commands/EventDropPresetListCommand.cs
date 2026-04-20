using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropPresetListCommand : IConsoleCommand
{
    public string Command => "drop_preset_list";
    public string Description => Loc.GetString("cmd-eventdrop-preset-list-desc");
    public string Help => Loc.GetString("cmd-eventdrop-preset-list-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var presetManager = IoCManager.Resolve<IEventDropPresetManager>();
        var presets = presetManager.GetAllPresetIds();
        
        if (presets.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-list-empty"));
            return;
        }
        
        shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-list-header", ("count", presets.Count)));
        
        foreach (var presetId in presets.OrderBy(x => x))
        {
            if (presetManager.TryGetPreset(presetId, out var preset))
            {
                var totalItems = preset.Items.Sum(x => x.Amount);
                var uniqueItems = preset.Items.Count;
                shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-list-item",
                    ("id", presetId),
                    ("total", totalItems),
                    ("unique", uniqueItems)));
                    
                if (!string.IsNullOrEmpty(preset.Description))
                    shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-list-item-desc", ("description", preset.Description)));
            }
            else
            {
                shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-list-item-error", ("id", presetId)));
            }
        }
    }
}