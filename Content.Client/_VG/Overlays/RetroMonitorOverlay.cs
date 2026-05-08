using Content.Shared._VG;
using Content.Shared._VG.Abilities;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._VG.Overlays;

public sealed class RetroMonitorOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _retroShader;

    public RetroMonitorOverlay()
    {
        IoCManager.InjectDependencies(this);
        _retroShader = _prototypeManager.Index<ShaderPrototype>("crt_vhs").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_playerManager.LocalEntity is not { Valid: true } player
            || !_entityManager.HasComponent<RetroMonitorViewComponent>(player))
        {
            return false;
        }

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        _retroShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        handle.UseShader(_retroShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}