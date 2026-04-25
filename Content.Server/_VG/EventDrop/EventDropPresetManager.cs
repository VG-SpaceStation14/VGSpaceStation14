using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Log;

namespace Content.Server._VG.EventDrop;

public interface IEventDropPresetManager
{
    bool TryGetPreset(string presetId, [NotNullWhen(true)] out EventDropPreset? preset);
    List<string> GetAllPresetIds();
    bool SavePreset(string presetId, EventDropPreset preset);
    bool DeletePreset(string presetId);
    bool PresetExists(string presetId);
    List<EntProtoId> GetItemsFromPreset(string presetId);
}

public sealed class EventDropPresetManager : IEventDropPresetManager, IPostInjectInit
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    
    private ISawmill _sawmill = default!;
    private readonly string _presetsPath = "data/eventdrop_presets.json";
    private EventDropPresetData _presetData = new();
    
    public void PostInject()
    {
        _sawmill = Logger.GetSawmill("eventdrop.presets");
        LoadPresets();
        _sawmill.Info($"EventDropPresetManager initialized with {_presetData.Presets.Count} presets");
    }
    
    private void LoadPresets()
    {
        try
        {
            if (!File.Exists(_presetsPath))
            {
                _presetData = new EventDropPresetData();
                SavePresetsInternal();
                _sawmill.Info($"Created new presets file at {_presetsPath}");
                return;
            }
            
            var json = File.ReadAllText(_presetsPath);
            _presetData = JsonSerializer.Deserialize<EventDropPresetData>(json) ?? new EventDropPresetData();
            _sawmill.Info($"Loaded {_presetData.Presets.Count} presets from {_presetsPath}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to load presets: {e}");
            _presetData = new EventDropPresetData();
        }
    }
    
    private void SavePresetsInternal()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_presetData, options);
            
            // Атомарное сохранение через временный файл
            var tempPath = _presetsPath + ".tmp";
            File.WriteAllText(tempPath, json);
            
            if (File.Exists(_presetsPath))
                File.Delete(_presetsPath);
                
            File.Move(tempPath, _presetsPath);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save presets: {e}");
        }
    }
    
    public bool TryGetPreset(string presetId, [NotNullWhen(true)] out EventDropPreset? preset)
    {
        return _presetData.Presets.TryGetValue(presetId, out preset);
    }
    
    public List<string> GetAllPresetIds()
    {
        return _presetData.Presets.Keys.ToList();
    }
    
    public bool SavePreset(string presetId, EventDropPreset preset)
    {
        try
        {
            // Валидация предметов
            foreach (var item in preset.Items)
            {
                if (!_prototypeManager.HasIndex<EntityPrototype>(item.PrototypeId))
                {
                    _sawmill.Warning($"Invalid prototype in preset '{presetId}': {item.PrototypeId}");
                    return false;
                }
            }
            
            preset.Name = presetId;
            _presetData.Presets[presetId] = preset;
            SavePresetsInternal();
            _sawmill.Info($"Saved preset '{presetId}' with {preset.Items.Count} items");
            return true;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save preset '{presetId}': {e}");
            return false;
        }
    }
    
    public bool DeletePreset(string presetId)
    {
        try
        {
            if (_presetData.Presets.Remove(presetId))
            {
                SavePresetsInternal();
                _sawmill.Info($"Deleted preset '{presetId}'");
                return true;
            }
            _sawmill.Warning($"Preset '{presetId}' not found for deletion");
            return false;
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to delete preset '{presetId}': {e}");
            return false;
        }
    }
    
    public bool PresetExists(string presetId)
    {
        return _presetData.Presets.ContainsKey(presetId);
    }
    
    public List<EntProtoId> GetItemsFromPreset(string presetId)
    {
        var items = new List<EntProtoId>();
        
        if (_presetData.Presets.TryGetValue(presetId, out var preset))
        {
            foreach (var presetItem in preset.Items)
            {
                for (int i = 0; i < presetItem.Amount; i++)
                {
                    items.Add(new EntProtoId(presetItem.PrototypeId));
                }
            }
        }
        
        return items;
    }
}