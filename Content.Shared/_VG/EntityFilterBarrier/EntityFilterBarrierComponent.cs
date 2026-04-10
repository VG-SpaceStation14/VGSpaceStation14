using Robust.Shared.GameStates;

namespace Content.Shared._VG.EntityFilterBarrier;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityFilterBarrierComponent : Component
{
    [DataField("bounds")]
    public Box2 Bounds = new(-0.5f, -0.5f, 0.5f, 0.5f);

    public readonly List<string> BlockedPrototypes = new()
    {
        "MobFleshLover", "MobFleshGolem", "MobFleshJared", "MobFleshClamp",
        "MobLuminousEntity", "MobLuminousObject", "MobLuminousPerson",
        "MobCoalCrab", "MobGoldCrab", "MobQuartzCrab",
        "MobSilverCrab", "MobTinCrab", "MobUraniumCrab",
        "MobBananiumCrab",
        "ReagentSlime", "ReagentSlimeBeer", "ReagentSlimePax", "ReagentSlimeNocturine",
        "ReagentSlimeTHC", "ReagentSlimeBicaridine", "ReagentSlimeToxin", "ReagentSlimeNapalm",
        "ReagentSlimeOmnizine", "ReagentSlimeMuteToxin", "ReagentSlimeNorepinephricAcid",
        "ReagentSlimeEphedrine", "ReagentSlimeRobustHarvest",
    };
}