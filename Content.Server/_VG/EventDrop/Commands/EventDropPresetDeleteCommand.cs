using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._VG.EventDrop;

[AdminCommand(AdminFlags.Fun)]
public sealed class EventDropPresetDeleteCommand : IConsoleCommand
{
    public string Command => "drop_preset_delete";
    public string Description => Loc.GetString("cmd-eventdrop-preset-delete-desc");
    public string Help => Loc.GetString("cmd-eventdrop-preset-delete-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-delete-error-id"));
            return;
        }

        var presetId = args[0];
        var presetManager = IoCManager.Resolve<IEventDropPresetManager>();
        
        if (!presetManager.PresetExists(presetId))
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-delete-error-notfound", ("preset", presetId)));
            return;
        }
        
        if (presetManager.DeletePreset(presetId))
        {
            shell.WriteLine(Loc.GetString("cmd-eventdrop-preset-delete-success", ("preset", presetId)));
        }
        else
        {
            shell.WriteError(Loc.GetString("cmd-eventdrop-preset-delete-error-failed", ("preset", presetId)));
        }
    }
}