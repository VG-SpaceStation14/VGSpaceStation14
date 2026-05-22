using Content.Shared.Light;
using Content.Shared.PDA;
using Robust.Client.GameObjects;

namespace Content.Client.PDA;

public sealed class PdaVisualizerSystem : VisualizerSystem<PdaVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, PdaVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        const int baseLayer = 0;
        const int flashlightLayer = 1;
        const int idLightLayer = 2;
        const int overlayLayer = 3;

        if (AppearanceSystem.TryGetData<string>(uid, PdaVisuals.PdaType, out var pdaType, args.Component))
            args.Sprite.LayerSetState(baseLayer, pdaType);

        if (AppearanceSystem.TryGetData<bool>(uid, UnpoweredFlashlightVisuals.LightOn, out var isFlashlightOn, args.Component))
            args.Sprite.LayerSetVisible(flashlightLayer, isFlashlightOn);

        if (AppearanceSystem.TryGetData<bool>(uid, PdaVisuals.IdCardInserted, out var isCardInserted, args.Component))
            args.Sprite.LayerSetVisible(idLightLayer, isCardInserted);

        if (AppearanceSystem.TryGetData<string>(uid, PdaVisuals.ScreenOverlay, out var overlay, args.Component))
        {
            args.Sprite.LayerSetState(overlayLayer, overlay);
            args.Sprite.LayerSetVisible(overlayLayer, true);
        }
        else
        {
            args.Sprite.LayerSetVisible(overlayLayer, false);
        }
    }
}