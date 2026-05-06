using Content.Shared._VG.Blink;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._VG.Blink;

public sealed class BlinkClientSystem : EntitySystem
{
    private readonly Dictionary<EntityUid, Color?> _cache = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<BlinkComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(EntityUid uid, BlinkComponent blink, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!sprite.LayerMapTryGet(blink.EyeLayer, out var layer))
            return;

        var color = blink.EyesClosed ? humanoid.SkinColor : humanoid.EyeColor;

        if (_cache.TryGetValue(uid, out var prev) && prev == color)
            return;

        _cache[uid] = color;
        sprite.LayerSetColor(layer, color);
    }
}