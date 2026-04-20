using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._VG.EventDrop;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropPresetLoadCommand : IConsoleCommand
{
    public string Command => "drop_preset_load";
    public string Description => Loc.GetString("cmd-eventdrop-preset-load-desc");
    public string Help => Loc.GetString("cmd-eventdrop-preset-load-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var player = shell.Player;
        if (player?.AttachedEntity == null) return;

        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-load-error-id"));
            return;
        }

        var presetId = args[0];
        
        var entMan = IoCManager.Resolve<IEntityManager>();
        var actor = player.AttachedEntity.Value;
        var presetManager = IoCManager.Resolve<IEventDropPresetManager>();
        
        if (!presetManager.TryGetPreset(presetId, out var preset))
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-load-error-notfound", ("preset", presetId)));
            return;
        }
        
        var comp = entMan.EnsureComponent<EventDropComponent>(actor);
        comp.PreparedItems.Clear();
        
        var items = presetManager.GetItemsFromPreset(presetId);
        comp.PreparedItems.AddRange(items);
        comp.CurrentPresetName = presetId;
        
        shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-load-success",
            ("preset", presetId),
            ("count", items.Count)));
            
        if (!string.IsNullOrEmpty(preset.Description))
            shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-load-success-desc", ("description", preset.Description)));
            
        shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-load-success-meta",
            ("author", preset.CreatedBy),
            ("date", preset.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))));
    }
}