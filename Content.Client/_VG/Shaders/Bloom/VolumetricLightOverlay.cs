using System.Numerics;
using Content.Shared._VG;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Configuration;

namespace Content.Client._VG.Shaders.Bloom;

public sealed partial class VolumetricLightOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _cataractsShader;

    public VolumetricLightOverlay()
    {
        IoCManager.InjectDependencies(this);
        _cataractsShader = _prototypeManager.Index<ShaderPrototype>("Cataracts").Instance().Duplicate();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player)
            return false;

        if (!_cfg.GetCVar(VGCCVars.BloomEnabled))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        if (args.Viewport.LightRenderTarget?.Texture == null)
            return;

        var player = _playerManager.LocalEntity;
        if (player == null)
            return;

        float zoom = 1f;
        if (_entityManager.TryGetComponent<EyeComponent>(player, out var eyeComp))
            zoom = eyeComp.Zoom.X;

        // Сила объемного свечения (CVar, по умолчанию 0.15 – как при лёгком повреждении)
        float strength = _cfg.GetCVar(VGCCVars.VolumetricLightStrength);
        
        // Настройки шейдера Cataracts:
        // DistortionScalar = 0 – без искажений
        // CloudinessScalar = strength – небольшое помутнение (нужно для активации свечения)
        _cataractsShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _cataractsShader.SetParameter("LIGHT_TEXTURE", args.Viewport.LightRenderTarget.Texture);
        _cataractsShader.SetParameter("Zoom", zoom);
        _cataractsShader.SetParameter("DistortionScalar", 0f);
        _cataractsShader.SetParameter("CloudinessScalar", strength);

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_cataractsShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }
}