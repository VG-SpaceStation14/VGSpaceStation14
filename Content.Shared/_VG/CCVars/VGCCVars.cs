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
}