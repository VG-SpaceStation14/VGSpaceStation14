using System.Text.Json.Serialization;
using Robust.Shared.Network;

namespace Content.Server._VG.Sponsors;

public sealed class SponsorEntry
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int Tier { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Notes { get; set; }

    [JsonIgnore]
    public NetUserId? NetUserId => Guid.TryParse(UserId, out var guid) ? new NetUserId(guid) : null;
}