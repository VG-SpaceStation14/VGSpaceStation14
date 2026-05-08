using System.Numerics;
using Content.Shared._VG;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._VG.Shaders.Bloom;

public sealed class LightingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private ConeLightingOverlay _cone = default!;
    private PointLightingOverlay _point = default!;

    private static readonly ProtoId<ShaderPrototype> Shader = "LightingOverlay";

    private bool _allEnabled;
    private bool _coneEnabled;
    private bool _bloomDisabled;

    private float _strength;

    private ConfigurationMultiSubscriptionBuilder _configSub = default!;

    private readonly List<(TransformComponent xform, Matrix3x2 matrix, Vector2 worldPos, Color color)> _entities = [];

    public override void Initialize()
    {
        base.Initialize();

        _cone = new ConeLightingOverlay(_prototypeManager, _sprite, Shader);
        _point = new PointLightingOverlay(_prototypeManager, _sprite, Shader);

        _transformQuery = GetEntityQuery<TransformComponent>();
        _eyeQuery = GetEntityQuery<EyeComponent>();

        _configSub = _cfg.SubscribeMultiple()
            .OnValueChanged(VGCCVars.LightBloomEnable, OnAllEnabledChanged, true)
            .OnValueChanged(VGCCVars.LightBloomConeEnable, OnConeEnabledChanged, true)
            .OnValueChanged(VGCCVars.LightBloomStrength, OnStrengthChanged, true)
            .OnValueChanged(VGCCVars.NoBloomPostProcessing, OnNoBloomChanged, true);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity == null)
            return;

        if (!_allEnabled || _bloomDisabled)
            return;

        _entities.Clear();

        var query = EntityQueryEnumerator<BloomOverlayVisualsComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var pointLight, out var xform))
        {
            if (!pointLight.Enabled)
                continue;

            var (worldPos, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(xform, _transformQuery);

            _entities.Add((xform, worldMatrix, worldPos, pointLight.Color));
        }

        _cone.Entities = _entities;
        _point.Entities = _entities;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_cone);
        _cone.Dispose();

        _overlayManager.RemoveOverlay(_point);
        _point.Dispose();

        _configSub.Dispose();
    }

    private void OnAllEnabledChanged(bool value)
    {
        _allEnabled = value;
        UpdateOverlays();
    }

    private void OnConeEnabledChanged(bool value)
    {
        _coneEnabled = value;
        UpdateOverlays();
    }

    private void OnStrengthChanged(float value)
    {
        _strength = Math.Clamp(value, 0.1f, 1f);

        _cone.Strength = _strength;
        _point.Strength = _strength;
    }
    
    private void OnNoBloomChanged(bool value)
    {
        _bloomDisabled = value;
        UpdateOverlays();
    }

    private void UpdateOverlays()
    {
        var shouldEnableCone = _allEnabled && _coneEnabled && !_bloomDisabled;
        var shouldEnablePoint = _allEnabled && !_bloomDisabled;

        _cone.Enabled = shouldEnableCone;
        _point.Enabled = shouldEnablePoint;

        ToggleOverlay(shouldEnableCone, _cone);
        ToggleOverlay(shouldEnablePoint, _point);
    }

    private void ToggleOverlay(bool value, Overlay overlay)
    {
        var hasOverlay = _overlayManager.HasOverlay(overlay.GetType());

        if (value && !hasOverlay)
            _overlayManager.AddOverlay(overlay);
        else if (!value && hasOverlay)
            _overlayManager.RemoveOverlay(overlay);
    }
}