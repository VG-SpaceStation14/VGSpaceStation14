// Based on Corvax Sponsors system

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.ADT.SponsorLoadout;
using Content.Server.Database;
using Content.Shared._VG.Sponsors;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._VG.Sponsors;

public sealed class SponsorsManager : ISponsorsManager
{
    [Dependency] private readonly IServerNetManager _netMgr = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ISawmill _sawmill = default!;
    private SponsorsDataHandler _dataHandler = default!;

    private readonly Dictionary<NetUserId, SponsorInfo> _cachedSponsors = new();

    // Pending actions for offline users (username -> list of actions)
    private Dictionary<string, List<PendingSponsorAction>> _pendingActions = new();
    private readonly string _pendingActionsPath = "data/pending_sponsor_actions.json";

    private static readonly Dictionary<int, string> TierColors = new()
    {
        { 1, "#33ccff" },
        { 2, "#3366ff" }, 
        { 3, "#9933ff" } 
    };

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("sponsors");
        _dataHandler = new SponsorsDataHandler(_sawmill);

        LoadPendingActions();

        _netMgr.RegisterNetMessage<MsgSponsorInfo>();

        _netMgr.Connecting += OnConnecting;
        _netMgr.Connected += OnConnected;
        _netMgr.Disconnect += OnDisconnect;

        IoCManager.Register<ISponsorsManager, SponsorsManager>(true);

        _sawmill.Info("SponsorsManager initialized with local JSON storage and pending actions");
    }

    #region Pending Actions

    private void LoadPendingActions()
    {
        try
        {
            if (!File.Exists(_pendingActionsPath))
            {
                _pendingActions = new Dictionary<string, List<PendingSponsorAction>>();
                return;
            }

            var json = File.ReadAllText(_pendingActionsPath);
            _pendingActions = JsonSerializer.Deserialize<Dictionary<string, List<PendingSponsorAction>>>(json)
                              ?? new Dictionary<string, List<PendingSponsorAction>>();
            _sawmill.Info($"Loaded {_pendingActions.Count} pending action entries from {_pendingActionsPath}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load pending actions: {e.Message}");
            _pendingActions = new Dictionary<string, List<PendingSponsorAction>>();
        }
    }

    private void SavePendingActions()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_pendingActions, options);
            File.WriteAllText(_pendingActionsPath, json);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save pending actions: {e.Message}");
        }
    }

    public void QueuePendingAction(PendingSponsorAction action)
    {
        var username = action.Username;
        if (!_pendingActions.ContainsKey(username))
            _pendingActions[username] = new List<PendingSponsorAction>();

        _pendingActions[username].Add(action);
        SavePendingActions();
        _sawmill.Info($"Queued {action.GetType().Name} for offline user '{username}'");
    }

    private void ProcessPendingActionsForUser(string username, NetUserId userId)
    {
        if (!_pendingActions.TryGetValue(username, out var actions))
            return;

        _sawmill.Info($"Processing {actions.Count} pending action(s) for user '{username}' (UID: {userId})");

        foreach (var action in actions)
        {
            try
            {
                switch (action)
                {
                    case AddSponsorAction add:
                        AddSponsor(userId, username, add.Tier, add.ExpireDate, add.Notes, null);
                        break;
                    case RemoveSponsorAction:
                        RemoveSponsor(userId);
                        break;
                    case AddLoadoutAction addLoadout:
                        AddCustomLoadout(userId, addLoadout.LoadoutId);
                        break;
                    case RemoveLoadoutAction removeLoadout:
                        RemoveCustomLoadout(userId, removeLoadout.LoadoutId);
                        break;
                    default:
                        _sawmill.Warning($"Unknown pending action type: {action.GetType()}");
                        break;
                }
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to process pending action {action.GetType().Name} for {username}: {e.Message}");
            }
        }

        _pendingActions.Remove(username);
        SavePendingActions();
    }

    #endregion

    #region Sponsor Info

    public bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsor);
    }

    bool ISponsorsManager.TryGetInfo([NotNullWhen(true)] out SponsorInfo? info)
    {
        info = null;
        return false;
    }

    #endregion

    #region Custom Loadouts

    public bool TryGetCustomLoadouts(NetUserId userId, [NotNullWhen(true)] out List<string>? customLoadouts)
    {
        customLoadouts = null;
        var entry = _dataHandler.GetSponsor(userId);
        
        if (entry == null || entry.CustomLoadouts == null || entry.CustomLoadouts.Count == 0)
            return false;
            
        customLoadouts = entry.CustomLoadouts;
        return true;
    }

    public void AddCustomLoadout(NetUserId userId, string loadoutId)
    {
        var entry = _dataHandler.GetRawSponsor(userId);
        if (entry == null)
        {
            _sawmill.Warning($"Cannot add custom loadout {loadoutId} to non-sponsor {userId}");
            return;
        }
        
        if (!entry.CustomLoadouts.Contains(loadoutId))
        {
            entry.CustomLoadouts.Add(loadoutId);
            _dataHandler.Save();
            _sawmill.Info($"Added custom loadout {loadoutId} to {entry.Username}");
            
            // Update cache and send to client
            if (_cachedSponsors.TryGetValue(userId, out var info))
            {
                info.CustomLoadouts = entry.CustomLoadouts.ToArray();
                if (_playerManager.TryGetSessionById(userId, out var session))
                {
                    var msg = new MsgSponsorInfo { Info = info };
                    _netMgr.ServerSendMessage(msg, session.Channel);
                }
            }
        }
    }

    public void RemoveCustomLoadout(NetUserId userId, string loadoutId)
    {
        var entry = _dataHandler.GetRawSponsor(userId);
        if (entry != null && entry.CustomLoadouts.Remove(loadoutId))
        {
            _dataHandler.Save();
            _sawmill.Info($"Removed custom loadout {loadoutId} from {entry.Username}");
            
            if (_cachedSponsors.TryGetValue(userId, out var info))
            {
                info.CustomLoadouts = entry.CustomLoadouts.ToArray();
                if (_playerManager.TryGetSessionById(userId, out var session))
                {
                    var msg = new MsgSponsorInfo { Info = info };
                    _netMgr.ServerSendMessage(msg, session.Channel);
                }
            }
        }
    }

    #endregion

    #region Connection Handling

    private async Task OnConnecting(NetConnectingArgs e)
    {
        // No longer set cache here; we will do it in OnConnected after processing pending actions
        await Task.CompletedTask;
    }

    private void OnConnected(object? sender, NetChannelArgs e)
    {
        var userId = e.Channel.UserId;
        if (!_playerManager.TryGetSessionById(userId, out var session))
        {
            _sawmill.Warning($"OnConnected: No session found for userId {userId}");
            return;
        }

        var username = session.Name;

        // Process any pending actions for this username
        ProcessPendingActionsForUser(username, userId);

        // Now load sponsor entry (which may have been added/updated by pending actions)
        var entry = _dataHandler.GetSponsor(userId);

        if (entry == null)
        {
            _cachedSponsors.Remove(userId);
            // Send null info to client (not a sponsor)
            var msg = new MsgSponsorInfo { Info = null };
            _netMgr.ServerSendMessage(msg, e.Channel);
            return;
        }

        var info = new SponsorInfo
        {
            Tier = entry.Tier,
            OOCColor = TierColors.GetValueOrDefault(entry.Tier, "#ffffff"),
            AllowJob = entry.Tier >= 2,
            ExtraSlots = entry.Tier >= 3 ? 2 : (entry.Tier >= 2 ? 1 : 0),
            ExpireDate = entry.ExpireDate ?? DateTime.MaxValue,
            CharacterName = entry.Username,
            CustomLoadouts = entry.CustomLoadouts.ToArray()
        };

        _cachedSponsors[userId] = info;
        _sawmill.Info($"Sponsor {username} (Tier {entry.Tier}) connected with {entry.CustomLoadouts.Count} custom loadouts");

        var msgSponsor = new MsgSponsorInfo { Info = info };
        _netMgr.ServerSendMessage(msgSponsor, e.Channel);
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

    #endregion

    #region Spawn Equipment

    public bool TryGetSpawnEquipment(NetUserId userId, string? jobPrototype, [NotNullWhen(true)] out string? spawnEquipment)
    {
        spawnEquipment = null;

        if (!TryGetInfo(userId, out var sponsorData))
            return false;

        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            var username = session.Name;
            var personalGears = _prototypeManager.EnumeratePrototypes<SponsorPersonalLoadoutPrototype>();
            var currentDate = DateTime.UtcNow;

            var jobLoadout = personalGears.FirstOrDefault(loadout =>
                loadout.UserName == username &&
                jobPrototype != null &&
                loadout.WhitelistJobs?.Contains(jobPrototype) == true &&
                (loadout.ExpirationDate == null || loadout.ExpirationDate > currentDate));

            if (jobLoadout != null)
            {
                spawnEquipment = jobLoadout.Equipment;
                return true;
            }

            var generalLoadout = personalGears.FirstOrDefault(loadout =>
                loadout.UserName == username &&
                (loadout.WhitelistJobs == null || loadout.WhitelistJobs.Count == 0) &&
                (loadout.ExpirationDate == null || loadout.ExpirationDate > currentDate));

            if (generalLoadout != null)
            {
                spawnEquipment = generalLoadout.Equipment;
                return true;
            }
        }

        var tierSettings = _prototypeManager.EnumeratePrototypes<SponsorLoadoutTierSettingPrototype>().FirstOrDefault();
        if (tierSettings != null && sponsorData.Tier.HasValue && tierSettings.Tiers.TryGetValue(sponsorData.Tier.Value, out var equipmentId))
        {
            spawnEquipment = equipmentId;
            return true;
        }

        return false;
    }

    #endregion

    #region Admin Commands (Direct)

    public void AddSponsor(NetUserId userId, string username, int tier, DateTime? expireDate = null, string? notes = null, List<string>? customLoadouts = null)
    {
        _dataHandler.AddOrUpdateSponsor(userId, username, tier, expireDate, notes, customLoadouts);
    
        var info = new SponsorInfo
        {
            Tier = tier,
            OOCColor = TierColors.GetValueOrDefault(tier, "#ffffff"),
            AllowJob = tier >= 2,
            ExtraSlots = tier >= 3 ? 2 : (tier >= 2 ? 1 : 0),
            ExpireDate = expireDate ?? DateTime.MaxValue,
            CharacterName = username,
            CustomLoadouts = customLoadouts?.ToArray() ?? Array.Empty<string>()
        };

        _cachedSponsors[userId] = info;
    
        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            var msg = new MsgSponsorInfo { Info = info };
            _netMgr.ServerSendMessage(msg, session.Channel);
            _sawmill.Info($"Sent updated sponsor info to {username} (Tier {tier})");
        }
    }

    public void RemoveSponsor(NetUserId userId)
    {
        _dataHandler.RemoveSponsor(userId);
        _cachedSponsors.Remove(userId);
    
        if (_playerManager.TryGetSessionById(userId, out var session))
        {
            var msg = new MsgSponsorInfo { Info = null };
            _netMgr.ServerSendMessage(msg, session.Channel);
            _sawmill.Info($"Sent removed sponsor info to {session.Name}");
        }
    }

    public List<SponsorEntry> ListSponsors()
    {
        return _dataHandler.GetAllSponsors();
    }

    #endregion
}