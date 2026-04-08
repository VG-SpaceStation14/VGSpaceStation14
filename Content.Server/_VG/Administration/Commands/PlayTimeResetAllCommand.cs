using System.Linq;
using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeResetAllCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "playtime_resetall_as";
    public string Description => Loc.GetString("cmd-playtime_resetall-desc");
    public string Help => Loc.GetString("cmd-playtime_resetall-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_resetall-error-args"));
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

        var trackers = _prototypeManager.EnumeratePrototypes<PlayTimeTrackerPrototype>();
        var resetRoles = new List<string>();
        var failedRoles = new List<string>();

        // Reset for online player
        if (_playerManager.TryGetSessionById(userId, out var player))
        {
            // Reset overall
            _playTimeTracking.ResetOverallPlaytime(player);

            // Reset all role trackers
            foreach (var tracker in trackers)
            {
                try
                {
                    _playTimeTracking.ResetPlaytimeForTracker(player, tracker.ID);

                    // Try to get localized name
                    var localized = Loc.GetString($"playtime-tracker-{tracker.ID}");
                    resetRoles.Add(localized);
                }
                catch
                {
                    failedRoles.Add(tracker.ID);
                }
            }
        }
        // Reset for offline player
        else
        {
            // Reset overall
            await _playTimeTracking.ResetOverallPlaytimeById(userId);

            // Reset all role trackers
            foreach (var tracker in trackers)
            {
                try
                {
                    await _playTimeTracking.ResetPlaytimeForTrackerById(userId, tracker.ID);

                    // Try to get localized name
                    var localized = Loc.GetString($"playtime-tracker-{tracker.ID}");
                    resetRoles.Add(localized);
                }
                catch
                {
                    failedRoles.Add(tracker.ID);
                }
            }
        }

        // Get updated overall time
        TimeSpan overall;
        if (_playerManager.TryGetSessionById(userId, out var finalPlayer))
        {
            overall = _playTimeTracking.GetOverallPlaytime(finalPlayer);
        }
        else
        {
            overall = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        // Format the result message
        var resultMessage = Loc.GetString("cmd-playtime_resetall-succeed",
            ("username", args[0]),
            ("rolescount", resetRoles.Count),
            ("overall", overall.TotalMinutes));

        if (failedRoles.Count > 0)
        {
            resultMessage += "\n" + Loc.GetString("cmd-playtime_resetall-failed",
                ("count", failedRoles.Count),
                ("trackers", string.Join(", ", failedRoles.Take(5))));
            if (failedRoles.Count > 5)
                resultMessage += Loc.GetString("cmd-playtime_resetall-more", ("count", failedRoles.Count - 5));
        }

        shell.WriteLine(resultMessage);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_resetall-arg-user"));
        }

        return CompletionResult.Empty;
    }
}