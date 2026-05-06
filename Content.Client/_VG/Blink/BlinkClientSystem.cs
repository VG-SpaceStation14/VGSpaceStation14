using Content.Shared._VG.Blink;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;

namespace Content.Client._VG.Blink;

public sealed class BlinkClientSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlinkComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, BlinkComponent blink, ref AfterAutoHandleStateEvent args)
    {
        UpdateBlinkVisuals(uid, blink);
    }

    private void UpdateBlinkVisuals(EntityUid uid, BlinkComponent blink)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!sprite.LayerMapTryGet(blink.EyeLayer, out var eyeLayer))
            return;

        var targetColor = blink.EyesClosed ? humanoid.SkinColor : humanoid.EyeColor;
        
        sprite.LayerSetColor(eyeLayer, targetColor);
    }
}