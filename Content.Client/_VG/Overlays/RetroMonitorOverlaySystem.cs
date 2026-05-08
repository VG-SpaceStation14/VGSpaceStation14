using Content.Shared._VG;
using Content.Shared._VG.Abilities;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._VG.Overlays;

public sealed class RetroMonitorOverlaySystem : EntitySystem
{
    [Dependency] private readonly GrainOverlaySystem _grain = default!;
    [Dependency] private readonly VignetteOverlaySystem _vignette = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly RetroMonitorOverlay _overlay = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RetroMonitorViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<RetroMonitorViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, VGCCVars.NoVisionFilters, OnNoVisionFiltersChanged);
    }

    private void OnPlayerAttached(Entity<RetroMonitorViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (!_cfg.GetCVar(VGCCVars.NoVisionFilters))
        {
            _overlayManager.AddOverlay(_overlay);

            _grain.RemoveOverlay();
            _vignette.RemoveOverlay();
        }
    }

    private void OnPlayerDetached(Entity<RetroMonitorViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);

        _grain.AddOverlay();
        _vignette.AddOverlay();
    }

    private void OnNoVisionFiltersChanged(bool enabled)
    {
        if (enabled)
        {
            _overlayManager.RemoveOverlay(_overlay);
            _grain.AddOverlay();
            _vignette.AddOverlay();
        }
        else
        {
            _overlayManager.AddOverlay(_overlay);
            _grain.RemoveOverlay();
            _vignette.RemoveOverlay();
        }
    }
}