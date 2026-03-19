// Based on Corvax Sponsors system

using System.Diagnostics.CodeAnalysis;
using System.Linq;
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

        _netMgr.RegisterNetMessage<MsgSponsorInfo>();

        _netMgr.Connecting += OnConnecting;
        _netMgr.Connected += OnConnected;
        _netMgr.Disconnect += OnDisconnect;

        IoCManager.Register<ISponsorsManager, SponsorsManager>(true);

        _sawmill.Info("SponsorsManager initialized with local JSON storage");
    }

    public bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        return _cachedSponsors.TryGetValue(userId, out sponsor);
    }

    bool ISponsorsManager.TryGetInfo([NotNullWhen(true)] out SponsorInfo? info)
    {
        info = null;
        return false;
    }

    private async Task OnConnecting(NetConnectingArgs e)
    {
        var entry = _dataHandler.GetSponsor(e.UserId);

        if (entry == null)
        {
            _cachedSponsors.Remove(e.UserId);
            return;
        }

        var info = new SponsorInfo
        {
            Tier = entry.Tier,
            OOCColor = TierColors.GetValueOrDefault(entry.Tier, "#ffffff"),
            AllowJob = entry.Tier >= 2,
            ExtraSlots = entry.Tier >= 3 ? 2 : (entry.Tier >= 2 ? 1 : 0),
            ExpireDate = entry.ExpireDate ?? DateTime.MaxValue,
            CharacterName = entry.Username 
        };

        DebugTools.Assert(!_cachedSponsors.ContainsKey(e.UserId), "Cached data was found on client connect");
        _cachedSponsors[e.UserId] = info;
        _sawmill.Info($"Sponsor {e.UserId} (Tier {entry.Tier}) connected");

        await Task.CompletedTask; 
    }

    private void OnConnected(object? sender, NetChannelArgs e) 
    {
        var info = _cachedSponsors.TryGetValue(e.Channel.UserId, out var sponsor) ? sponsor : null;
        var msg = new MsgSponsorInfo { Info = info };
        _netMgr.ServerSendMessage(msg, e.Channel);
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs e)
    {
        _cachedSponsors.Remove(e.Channel.UserId);
    }

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

    public void AddSponsor(NetUserId userId, string username, int tier, DateTime? expireDate = null, string? notes = null)
    {
        _dataHandler.AddOrUpdateSponsor(userId, username, tier, expireDate, notes);
        if (_cachedSponsors.ContainsKey(userId))
        {
            var info = new SponsorInfo
            {
                Tier = tier,
                OOCColor = TierColors.GetValueOrDefault(tier, "#ffffff"),
                AllowJob = tier >= 2,
                ExtraSlots = tier >= 3 ? 2 : (tier >= 2 ? 1 : 0),
                ExpireDate = expireDate ?? DateTime.MaxValue,
                CharacterName = username
            };
            _cachedSponsors[userId] = info;

            if (_playerManager.TryGetSessionById(userId, out var session))
            {
                var msg = new MsgSponsorInfo { Info = info };
                _netMgr.ServerSendMessage(msg, session.Channel);
            }
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
        }
    }

    public List<SponsorEntry> ListSponsors()
    {
        return _dataHandler.GetAllSponsors();
    }
}