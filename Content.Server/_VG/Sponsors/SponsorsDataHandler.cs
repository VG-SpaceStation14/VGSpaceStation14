using System.IO;
using System.Linq;
using System.Text.Json;
using Robust.Shared.Network;

namespace Content.Server._VG.Sponsors;

public sealed class SponsorsDataHandler
{
    private readonly ISawmill _sawmill;
    private readonly string _dataPath = "data/sponsors.json";
    private List<SponsorEntry> _sponsors = new();

    public SponsorsDataHandler(ISawmill sawmill)
    {
        _sawmill = sawmill;
        Load();
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(_dataPath))
            {
                _sawmill.Info("Sponsors file not found, creating new one");
                Save();
                return;
            }

            var json = File.ReadAllText(_dataPath);
            _sponsors = JsonSerializer.Deserialize<List<SponsorEntry>>(json) ?? new();
            _sawmill.Info($"Loaded {_sponsors.Count} sponsors from {_dataPath}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load sponsors: {e.Message}");
            _sponsors = new();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_sponsors, options);
            File.WriteAllText(_dataPath, json);
            _sawmill.Info($"Saved {_sponsors.Count} sponsors to {_dataPath}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save sponsors: {e.Message}");
        }
    }

    public SponsorEntry? GetSponsor(NetUserId userId)
    {
        var userIdStr = userId.ToString();
        var entry = _sponsors.FirstOrDefault(s => s.UserId == userIdStr);
        if (entry == null)
            return null;

        // Check expiration
        if (entry.ExpireDate != null && entry.ExpireDate < DateTime.UtcNow)
            return null;

        return entry;
    }

    public SponsorEntry? GetRawSponsor(NetUserId userId)
    {
        var userIdStr = userId.ToString();
        return _sponsors.FirstOrDefault(s => s.UserId == userIdStr);
    }

    public void AddOrUpdateSponsor(NetUserId userId, string username, int tier, 
        DateTime? expireDate = null, string? notes = null, 
        List<string>? customLoadouts = null, string? oocColor = null)
    {
        var userIdStr = userId.ToString();
        var existing = _sponsors.FirstOrDefault(s => s.UserId == userIdStr);
        if (existing != null)
        {
            existing.Tier = tier;
            existing.ExpireDate = expireDate;
            existing.Notes = notes;
            existing.Username = username;
            if (customLoadouts != null)
                existing.CustomLoadouts = customLoadouts;
            if (oocColor != null)
                existing.OOCColor = oocColor;
        }
        else
        {
            _sponsors.Add(new SponsorEntry
            {
                UserId = userIdStr,
                Username = username,
                Tier = tier,
                ExpireDate = expireDate,
                Notes = notes,
                CustomLoadouts = customLoadouts ?? new(),
                OOCColor = oocColor
            });
        }
        Save();
    }

    public void RemoveSponsor(NetUserId userId)
    {
        var userIdStr = userId.ToString();
        _sponsors.RemoveAll(s => s.UserId == userIdStr);
        Save();
    }

    public List<SponsorEntry> GetAllSponsors()
    {
        return _sponsors.ToList();
    }
}