using System.Linq;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._VG.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeResetOverallCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;

    public string Command => "playtime_resetoverall_as";
    public string Description => Loc.GetString("cmd-playtime_resetoverall-desc");
    public string Help => Loc.GetString("cmd-playtime_resetoverall-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_resetoverall-error-args"));
            return;
        }

        // Parse user
        NetUserId userId;
        if (Guid.TryParse(args[0], out var guid))
        {
            userId = new NetUserId(guid);
        }
        else
        {
            var dbGuid = await _playerLocator.LookupIdByNameAsync(args[0]);
            if (dbGuid == null)
            {
                shell.WriteError(Loc.GetString("parse-session-fail", ("username", args[0])));
                return;
            }
            userId = dbGuid.UserId;
        }

        // Get overall before reset
        TimeSpan overallBefore;
        if (_playerManager.TryGetSessionById(userId, out var prePlayer))
        {
            overallBefore = _playTimeTracking.GetOverallPlaytime(prePlayer);
        }
        else
        {
            overallBefore = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        // Reset overall playtime
        if (_playerManager.TryGetSessionById(userId, out var player))
        {
            _playTimeTracking.ResetOverallPlaytime(player);
        }
        else
        {
            await _playTimeTracking.ResetOverallPlaytimeById(userId);
        }

        // Get updated overall time
        TimeSpan overallAfter;
        if (_playerManager.TryGetSessionById(userId, out var finalPlayer))
        {
            overallAfter = _playTimeTracking.GetOverallPlaytime(finalPlayer);
        }
        else
        {
            overallAfter = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        // Format the result message
        var resultMessage = Loc.GetString("cmd-playtime_resetoverall-succeed",
            ("username", args[0]),
            ("before", overallBefore.TotalMinutes),
            ("after", overallAfter.TotalMinutes));

        shell.WriteLine(resultMessage);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_resetoverall-arg-user"));
        }

        return CompletionResult.Empty;
    }
}