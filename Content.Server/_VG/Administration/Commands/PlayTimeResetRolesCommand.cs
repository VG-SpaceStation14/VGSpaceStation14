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
public sealed class PlayTimeResetRolesCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "playtime_resetroles_as";
    public string Description => Loc.GetString("cmd-playtime_resetroles-desc");
    public string Help => Loc.GetString("cmd-playtime_resetroles-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-playtime_resetroles-error-args"));
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

        // Save overall playtime before resetting roles
        TimeSpan overallBefore;
        if (_playerManager.TryGetSessionById(userId, out var prePlayer))
        {
            overallBefore = _playTimeTracking.GetOverallPlaytime(prePlayer);
        }
        else
        {
            overallBefore = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        var trackers = _prototypeManager.EnumeratePrototypes<PlayTimeTrackerPrototype>();
        var resetRoles = new List<string>();
        var failedRoles = new List<string>();

        // Reset role trackers (skip overall)
        foreach (var tracker in trackers)
        {
            if (tracker.ID == PlayTimeTrackingShared.TrackerOverall)
                continue;

            try
            {
                if (_playerManager.TryGetSessionById(userId, out var onlinePlayer))
                {
                    _playTimeTracking.ResetPlaytimeForTracker(onlinePlayer, tracker.ID);
                }
                else
                {
                    await _playTimeTracking.ResetPlaytimeForTrackerById(userId, tracker.ID);
                }

                // Try to get localized name
                var localized = Loc.GetString($"playtime-tracker-{tracker.ID}");
                resetRoles.Add(localized);
            }
            catch
            {
                failedRoles.Add(tracker.ID);
            }
        }

        // Restore overall playtime if it was affected
        if (_playerManager.TryGetSessionById(userId, out var player))
        {
            var currentOverall = _playTimeTracking.GetOverallPlaytime(player);
            if (currentOverall != overallBefore)
            {
                var difference = overallBefore - currentOverall;
                if (difference > TimeSpan.Zero)
                {
                    _playTimeTracking.AddTimeToOverallPlaytime(player, difference);
                }
            }
        }
        else
        {
            var currentOverall = await _playTimeTracking.GetOverallPlaytimeById(userId);
            if (currentOverall != overallBefore)
            {
                var difference = overallBefore - currentOverall;
                if (difference > TimeSpan.Zero)
                {
                    await _playTimeTracking.AddTimeToOverallPlaytimeById(userId, difference);
                }
            }
        }

        // Get final overall time
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
        var resultMessage = Loc.GetString("cmd-playtime_resetroles-succeed",
            ("username", args[0]),
            ("rolescount", resetRoles.Count),
            ("overall", overallAfter.TotalMinutes));

        if (failedRoles.Count > 0)
        {
            resultMessage += "\n" + Loc.GetString("cmd-playtime_resetroles-failed",
                ("count", failedRoles.Count),
                ("trackers", string.Join(", ", failedRoles.Take(5))));
            if (failedRoles.Count > 5)
                resultMessage += Loc.GetString("cmd-playtime_resetroles-more", ("count", failedRoles.Count - 5));
        }

        shell.WriteLine(resultMessage);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-playtime_resetroles-arg-user"));
        }

        return CompletionResult.Empty;
    }
}