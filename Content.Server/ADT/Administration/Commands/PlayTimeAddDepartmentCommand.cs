// Content.Server/ADT/Administration/Commands/PlayTimeAddDepartmentCommand.cs

using System.Linq;
using Content.Server.Administration;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimeAddDepartmentCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public string Command => "playtime_adddepartment_as";
    public string Description => "Adds playtime to a specific department for a player (distributed equally between roles)";
    public string Help => $"Usage: {Command} <username/guid> <department> <minutes>\n" +
                          $"Example: {Command} john Security 60\n" +
                          $"Example: {Command} 12345678-1234-1234-1234-123456789012 Engineering 30\n" +
                          $"Note: Time is distributed equally between all roles in the department";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteError("Invalid number of arguments. Expected: <username/guid> <department> <minutes>");
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
        if (!int.TryParse(args[2], out var totalMinutes) || totalMinutes < 0)
        {
            shell.WriteError($"Invalid minutes value: {args[2]}");
            return;
        }

        // Find department and its roles
        var departmentId = args[1];
        if (!_prototypeManager.TryIndex<DepartmentPrototype>(departmentId, out var department))
        {
            shell.WriteError($"Department not found: {departmentId}");
            return;
        }

        var timeToAddTotal = TimeSpan.FromMinutes(totalMinutes);
        var addedRoles = new List<string>();
        var failedRoles = new List<string>();
        
        // Get all valid roles in the department
        var validRoles = new List<(string Tracker, string LocalizedName)>();
        
        foreach (var jobId in department.Roles)
        {
            if (!_prototypeManager.TryIndex<JobPrototype>(jobId, out var job))
            {
                failedRoles.Add(jobId);
                continue;
            }

            var tracker = job.PlayTimeTracker;
            if (string.IsNullOrEmpty(tracker))
            {
                failedRoles.Add(job.LocalizedName);
                continue;
            }

            validRoles.Add((tracker, job.LocalizedName));
        }

        if (validRoles.Count == 0)
        {
            shell.WriteError($"No valid roles found in department: {departmentId}");
            return;
        }

        // Distribute time equally between all valid roles
        var timePerRole = TimeSpan.FromMinutes(totalMinutes / (double)validRoles.Count);
        var minutesPerRole = totalMinutes / (double)validRoles.Count;
        
        // Add time to each role
        foreach (var (tracker, localizedName) in validRoles)
        {
            if (_playerManager.TryGetSessionById(userId, out var player))
            {
                _playTimeTracking.AddTimeToTracker(player, tracker, timePerRole);
            }
            else
            {
                await _playTimeTracking.AddTimeToTrackerById(userId, tracker, timePerRole);
            }
            
            addedRoles.Add(localizedName);
        }

        // Get updated overall time
        TimeSpan overall;
        if (_playerManager.TryGetSessionById(userId, out var onlinePlayer))
        {
            overall = _playTimeTracking.GetOverallPlaytime(onlinePlayer);
        }
        else
        {
            overall = await _playTimeTracking.GetOverallPlaytimeById(userId);
        }

        // Format the result message
        var resultMessage = $"Successfully added {totalMinutes} minutes total to department '{departmentId}' for {args[0]}\n" +
                           $"Time distributed equally between {validRoles.Count} roles\n" +
                           $"Added {minutesPerRole:F2} minutes to each role";

        if (addedRoles.Count > 0)
        {
            resultMessage += $"\nUpdated roles: {string.Join(", ", addedRoles)}";
        }

        if (failedRoles.Count > 0)
        {
            resultMessage += $"\nFailed to update roles: {string.Join(", ", failedRoles)}";
        }

        resultMessage += $"\nOverall playtime: {overall.TotalMinutes:F0} minutes";

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
            var departments = _prototypeManager.EnumeratePrototypes<DepartmentPrototype>()
                .Select(d => d.ID)
                .OrderBy(id => id)
                .ToArray();
            
            return CompletionResult.FromHintOptions(
                departments,
                "Department ID"
            );
        }

        if (args.Length == 3)
        {
            return CompletionResult.FromHint("Minutes to add (total)");
        }

        return CompletionResult.Empty;
    }
}