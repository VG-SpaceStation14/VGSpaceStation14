using System.Linq;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Sprite; // VG-Tweak
using Robust.Client.GameObjects;
using Robust.Shared.Containers; // VG-Tweak
using Robust.Shared.Utility;
using Robust.Client.ResourceManagement; // VG-Tweak
using Robust.Client.Graphics; // VG-Tweak

namespace Content.Client.Storage.Systems;

public sealed class ItemMapperSystem : SharedItemMapperSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!; // Кэш ресурсов для проверки RSI

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemMapperComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ItemMapperComponent, AppearanceChangeEvent>(OnAppearance);
    }

    // VG-Tweak Start
    private void OnStartup(EntityUid uid, ItemMapperComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out SpriteComponent? sprite))
        {
            component.RSIPath ??= sprite.BaseRSI!.Path;
        }
    }
    // VG-Tweak End

    // VG-Tweak Start
    private void OnAppearance(EntityUid uid, ItemMapperComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp(uid, out SpriteComponent? spriteComponent))
            return;

        if (component.SpriteLayers.Count == 0)
        {
            InitLayers(uid, component, spriteComponent, args.Component);
        }

        EnableLayers(uid, component, spriteComponent, args.Component);
    }
    // VG-Tweak End

    // VG-Tweak Start
    private void InitLayers(
        EntityUid uid,
        ItemMapperComponent component,
        SpriteComponent spriteComponent,
        AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<ShowLayerData>(
                uid,
                StorageMapVisuals.InitLayers,
                out var wrapper,
                appearance))
        {
            return;
        }

        component.SpriteLayers.AddRange(wrapper.QueuedEntities);

        foreach (var sprite in component.SpriteLayers)
        {
            _sprite.LayerMapReserve((uid, spriteComponent), sprite);

            _sprite.LayerSetSprite(
                (uid, spriteComponent),
                sprite,
                new SpriteSpecifier.Rsi(component.RSIPath!.Value, sprite));

            _sprite.LayerSetVisible((uid, spriteComponent), sprite, false);
        }

        if (HasState(component.RSIPath!.Value, "cutters_handle"))
            CreateCustomLayer(uid, spriteComponent, component, "cutters_handle");

        if (HasState(component.RSIPath!.Value, "screwdriver"))
            CreateCustomLayer(uid, spriteComponent, component, "screwdriver");
    }
    // VG-Tweak End

    // VG-Tweak Start
    private void CreateCustomLayer(
        EntityUid uid,
        SpriteComponent sprite,
        ItemMapperComponent component,
        string layer)
    {
        _sprite.LayerMapReserve((uid, sprite), layer);

        _sprite.LayerSetSprite(
            (uid, sprite),
            layer,
            new SpriteSpecifier.Rsi(component.RSIPath!.Value, layer));

        _sprite.LayerSetVisible((uid, sprite), layer, false);
    }
    // VG-Tweak End

    private bool HasState(ResPath rsiPath, string state)
    {
        if (_resourceCache.TryGetResource<RSIResource>(rsiPath, out var rsiResource))
        {
            return rsiResource.RSI.TryGetState(state, out _);
        }
        return false;
    }

    // VG-Tweak Start
    private void EnableLayers(
        EntityUid uid,
        ItemMapperComponent component,
        SpriteComponent spriteComponent,
        AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<ShowLayerData>(
                uid,
                StorageMapVisuals.LayerChanged,
                out var wrapper,
                appearance))
        {
            return;
        }

        foreach (var layerName in component.SpriteLayers)
        {
            var show = wrapper.QueuedEntities.Contains(layerName);
            _sprite.LayerSetVisible((uid, spriteComponent), layerName, show);
        }

        if (_sprite.LayerExists((uid, spriteComponent), "cutters_handle"))
        {
            _sprite.LayerSetVisible((uid, spriteComponent), "cutters_handle", false);
            _sprite.LayerSetColor((uid, spriteComponent), "cutters_handle", Color.White);
        }

        if (_sprite.LayerExists((uid, spriteComponent), "screwdriver"))
        {
            _sprite.LayerSetVisible((uid, spriteComponent), "screwdriver", false);
            _sprite.LayerSetColor((uid, spriteComponent), "screwdriver", Color.White);
        }

        if (!TryComp(uid, out ContainerManagerComponent? containers))
            return;

        if (!containers.TryGetContainer("storagebase", out var container))
            return;

        foreach (var entity in container.ContainedEntities)
        {
            if (!TryComp(entity, out RandomSpriteComponent? randomSprite))
                continue;

            if (!randomSprite.Selected.TryGetValue(
                    "enum.DamageStateVisualLayers.Base",
                    out var data))
                continue;

            var color = data.Color ?? Color.White;
            var proto = MetaData(entity).EntityPrototype?.ID ?? "";

            if (proto == "Wirecutter" && _sprite.LayerExists((uid, spriteComponent), "cutters_handle"))
            {
                _sprite.LayerSetVisible((uid, spriteComponent), "cutters_handle", true);
                _sprite.LayerSetColor((uid, spriteComponent), "cutters_handle", color);
            }

            if (proto == "Screwdriver" && _sprite.LayerExists((uid, spriteComponent), "screwdriver"))
            {
                _sprite.LayerSetVisible((uid, spriteComponent), "screwdriver", true);
                _sprite.LayerSetColor((uid, spriteComponent), "screwdriver", color);
            }
        }
    }
    // VG-Tweak End
}