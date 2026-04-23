using System.Linq;
using Content.Shared._VG.Mood;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._VG.Mood;

public sealed class MoodVisualizerSystem : VisualizerSystem<MoodVisualsComponent>
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoodVisualsComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MoodVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<MoodVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (_spriteSystem.LayerMapTryGet((ent.Owner, sprite), MoodVisualLayers.Mood, out var layer, false))
            _spriteSystem.RemoveLayer((ent.Owner, sprite), layer);
    }

    private void OnComponentInit(Entity<MoodVisualsComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        _spriteSystem.LayerMapReserve((ent.Owner, sprite), MoodVisualLayers.Mood);
        _spriteSystem.LayerSetVisible((ent.Owner, sprite), MoodVisualLayers.Mood, false);
        sprite.LayerSetShader(MoodVisualLayers.Mood, "unshaded");

        if (ent.Comp.Sprite != null)
        {
            var rsiPath = new ResPath(ent.Comp.Sprite);

            var initialState = ent.Comp.MoodStates.Count > 0
                ? ent.Comp.MoodStates.Values.First()
                : "default";

            var specifier = new SpriteSpecifier.Rsi(rsiPath, initialState);
            _spriteSystem.LayerSetSprite((ent.Owner, sprite), MoodVisualLayers.Mood, specifier);
        }

        if (TryComp<AppearanceComponent>(ent.Owner, out var appearance))
            UpdateAppearance(ent, sprite, appearance);
    }

    protected override void OnAppearanceChange(EntityUid uid, MoodVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null)
            UpdateAppearance((uid, component), args.Sprite, args.Component);
    }

    private bool ShouldHideMoodVisuals(Entity<MoodVisualsComponent> ent)
    {
        return HasComp<HideMoodVisualsComponent>(ent);
    }

    private void UpdateAppearance(Entity<MoodVisualsComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_spriteSystem.LayerMapTryGet((ent, sprite), MoodVisualLayers.Mood, out var index, false))
            return;

        if (ShouldHideMoodVisuals(ent))
        {
            _spriteSystem.LayerSetVisible((ent, sprite), index, false);
            return;
        }

        if (!_appearanceSystem.TryGetData<MoodThreshold>(
                ent.Owner,
                MoodVisuals.CurrentMoodThreshold,
                out var moodThreshold,
                appearance))
        {
            _spriteSystem.LayerSetVisible((ent.Owner, sprite), index, false);
            return;
        }

        if (!ent.Comp.MoodStates.TryGetValue(moodThreshold, out var state))
        {
            _spriteSystem.LayerSetVisible((ent.Owner, sprite), index, false);
            return;
        }

        _spriteSystem.LayerSetVisible((ent.Owner, sprite), index, true);
        _spriteSystem.LayerSetRsiState((ent.Owner, sprite), index, state);
    }
}