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

    // Vision filters
    public static readonly CVarDef<bool> NoVisionFilters =
        CVarDef.Create("vg.no_vision_filters", false, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> NoBloomPostProcessing =
        CVarDef.Create("vg.no_bloom_post_processing", false, CVar.CLIENT | CVar.ARCHIVE);

    // Grain
    public static readonly CVarDef<bool> GrainToggleOverlay =
        CVarDef.Create("vg.grain_toggle_overlay", true, CVar.CLIENT | CVar.ARCHIVE);

    // Light bloom
    public static readonly CVarDef<bool> LightBloomEnable =
        CVarDef.Create("vg.light_bloom_enable", true, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> LightBloomConeEnable =
        CVarDef.Create("vg.light_bloom_cone_enable", false, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<float> LightBloomStrength =
        CVarDef.Create("vg.light_bloom_strength", 0.1f, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CharacterSetupNewWindowEnabled =
        CVarDef.Create("vg.character_setup_new_window", false, CVar.CLIENT | CVar.ARCHIVE);
}