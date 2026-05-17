using Robust.Shared.Configuration;

namespace Content.Shared._VG;

[CVarDefs]
public sealed class VGCCVars
{
    public static readonly CVarDef<bool> MoodEnabled =
        CVarDef.Create("vg.mood_enabled", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> MoodIncreasesSpeed =
        CVarDef.Create("vg.mood_increases_speed", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> MoodDecreasesSpeed =
        CVarDef.Create("vg.mood_decreases_speed", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> VisionFiltersEnabled =
        CVarDef.Create("vg.vision_filters_enabled", true, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> BloomEnabled =
        CVarDef.Create("vg.bloom_enabled", true, CVar.CLIENT | CVar.ARCHIVE);

    // Grain
    public static readonly CVarDef<bool> GrainToggleOverlay =
        CVarDef.Create("vg.grain_toggle_overlay", true, CVar.CLIENT | CVar.ARCHIVE);

    // Light bloom 
    public static readonly CVarDef<bool> LightBloomConeEnable =
        CVarDef.Create("vg.light_bloom_cone_enable", false, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("vg.light_bloom_strength", 0.1f, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CharacterSetupNewWindowEnabled =
        CVarDef.Create("vg.character_setup_new_window", true, CVar.CLIENT | CVar.ARCHIVE);
    
    public static readonly CVarDef<float> VolumetricLightStrength =
    CVarDef.Create("vg.volumetric_light_strength", 0.007f, CVar.CLIENT | CVar.ARCHIVE);
}