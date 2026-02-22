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
    public string Description => "Adds playtime to a specific department for a player";
    public string Help => $"Usage: {Command} <username/guid> <department> <minutes>\n" +
                          $"Example: {Command} john Security 60\n" +
                          $"Example: {Command} 12345678-1234-1234-1234-123456789012 Engineering 30";

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
        if (!int.TryParse(args[2], out var minutes) || minutes < 0)
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

        var timeToAdd = TimeSpan.FromMinutes(minutes);
        var addedRoles = new List<string>();
        var failedRoles = new List<string>();

        // Add time to each role in the department
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

            if (_playerManager.TryGetSessionById(userId, out var player))
            {
                _playTimeTracking.AddTimeToTracker(player, tracker, timeToAdd);
            }
            else
            {
                await _playTimeTracking.AddTimeToTrackerById(userId, tracker, timeToAdd);
            }
            
            addedRoles.Add(job.LocalizedName);
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

        // Format the result message (ВСЕГДА В МИНУТАХ)
        var resultMessage = $"Successfully added {minutes} minutes to all roles in department '{departmentId}' for {args[0]}\n" +
                           $"Added to {addedRoles.Count} roles\n" +
                           $"Total department time added: {minutes * addedRoles.Count} minutes";

        if (addedRoles.Count > 0)
        {
            resultMessage += $"\nUpdated roles: {string.Join(", ", addedRoles)}";
        }

        if (failedRoles.Count > 0)
        {
            resultMessage += $"\nFailed to update roles: {string.Join(", ", failedRoles)}";
        }

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
            return CompletionResult.FromHint("Minutes to add");
        }

        return CompletionResult.Empty;
    }
}