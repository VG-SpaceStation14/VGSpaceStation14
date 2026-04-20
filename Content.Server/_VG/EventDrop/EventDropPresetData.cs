using System.Text.Json.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Server._VG.EventDrop;

public sealed class EventDropPresetData
{
    [JsonPropertyName("presets")]
    public Dictionary<string, EventDropPreset> Presets { get; set; } = new();
}

public sealed class EventDropPreset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("items")]
    public List<PresetItem> Items { get; set; } = new();
    
    [JsonPropertyName("created_by")]
    public string CreatedBy { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PresetItem
{
    [JsonPropertyName("prototype_id")]
    public string PrototypeId { get; set; } = string.Empty;
    
    [JsonPropertyName("amount")]
    public int Amount { get; set; } = 1;
}