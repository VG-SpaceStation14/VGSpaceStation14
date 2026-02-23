// Content.Server/ADT/Administration/Commands/PlayTimeAddRoleAsyncCommand.cs

using System.Linq;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddRoleAsyncCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "playtime_addrole_as";
    public string Description => "Adds playtime to a specific role for a player";
    public string Help => $"Usage: {Command} <username/guid> <role> <minutes>\n" +
                          $"Example: {Command} john SecurityOfficer 60\n" +
                          $"Example: {Command} 12345678-1234-1234-1234-123456789012 CargoTechnician 30";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError("Invalid number of arguments. Expected: <username/guid> <role> <minutes>");
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
                shell.WriteError($"Unable to find user: {args[0]}");
                return;
            }
            userId = dbGuid.UserId;
        }

        // Parse minutes
        if (!int.TryParse(args[2], out var minutes) || minutes < 0)
        {
            shell.WriteError($"Invalid minutes value: {args[2]}");
            return;
        }

        // Find the role tracker
        var roleId = args[1];
        string? trackerId = null;
        string? localizedRoleName = null;

        // Try to find as JobPrototype first
        if (_prototypeManager.TryIndex<JobPrototype>(roleId, out var job))
        {
            trackerId = job.PlayTimeTracker;
            localizedRoleName = job.LocalizedName;
        }
        // Try as PlayTimeTrackerPrototype directly
        else if (_prototypeManager.HasIndex<PlayTimeTrackerPrototype>(roleId))
        {
            trackerId = roleId;
            // Try to get localized name from tracker
            if (Loc.TryGetString($"playtime-tracker-{roleId}", out var localized))
                localizedRoleName = localized;
            else
                localizedRoleName = roleId;
        }
        else
        {
            shell.WriteError($"Role not found: {roleId}");
            return;
        }

        if (string.IsNullOrEmpty(trackerId))
        {
            shell.WriteError($"Invalid tracker for role: {roleId}");
            return;
        }

        var timeToAdd = TimeSpan.FromMinutes(minutes);

        // Add time to the specific role
        if (_playerManager.TryGetSessionById(userId, out var player))
        {
            _playTimeTracking.AddTimeToTracker(player, trackerId, timeToAdd);
        }
        else
        {
            await _playTimeTracking.AddTimeToTrackerById(userId, trackerId, timeToAdd);
        }

        // Get updated role time
        TimeSpan roleTime;
        if (_playerManager.TryGetSessionById(userId, out var onlinePlayer))
        {
            roleTime = _playTimeTracking.GetPlayTimeForTracker(onlinePlayer, trackerId);
        }
        else
        {
            roleTime = await _playTimeTracking.GetPlayTimeForTrackerById(userId, trackerId);
        }

        // Get updated overall time
        TimeSpan overall;
        if (_playerManager.TryGetSessionById(userId, out var overallPlayer))
        {
            overall = _playTimeTracking.GetOverallPlaytime(overallPlayer);
        }
        else
        {
            overall = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        // Format the result message
        var resultMessage = $"Successfully added {minutes} minutes to role '{localizedRoleName}' ({trackerId}) for {args[0]}\n" +
                           $"Role now has {roleTime.TotalMinutes:F0} minutes\n" +
                           $"Overall playtime: {overall.TotalMinutes:F0} minutes";

        shell.WriteLine(resultMessage);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                "Username or GUID"
            );
        }

        if (args.Length == 2)
        {
            // Get all job prototypes
            var jobRoles = _prototypeManager.EnumeratePrototypes<JobPrototype>()
                .Select(j => j.ID)
                .ToList();

            // Get all playtime trackers (excluding those already in jobs if needed)
            var trackerRoles = _prototypeManager.EnumeratePrototypes<PlayTimeTrackerPrototype>()
                .Select(t => t.ID)
                .Except(jobRoles) // Avoid duplicates if tracker IDs match job IDs
                .ToList();

            var allRoles = jobRoles.Concat(trackerRoles)
                .OrderBy(id => id)
                .ToArray();

            return CompletionResult.FromHintOptions(
                allRoles,
                "Role ID (Job or Tracker)"
            );
        }

        if (args.Length == 3)
        {
            return CompletionResult.FromHint("Minutes to add");
        }

        return CompletionResult.Empty;
    }
}