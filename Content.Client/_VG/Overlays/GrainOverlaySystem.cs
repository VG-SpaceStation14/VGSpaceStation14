using Content.Shared._VG;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._VG.Overlays;

public sealed class GrainOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private GrainOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        Subs.CVar(_cfg, VGCCVars.GrainToggleOverlay, OnGrainCvarChanged);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args) // Исправлено: LocalPlayerDetachedEvent
    {
        RemoveOverlay();
    }

    #region Public API

    public void ToggleOverlay()
    {
        if (_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.RemoveOverlay(_overlay);
        else if (_cfg.GetCVar(VGCCVars.GrainToggleOverlay))
            _overlayManager.AddOverlay(_overlay);
    }

    public void AddOverlay()
    {
        if (!_overlayManager.HasOverlay<GrainOverlay>() && _cfg.GetCVar(VGCCVars.GrainToggleOverlay))
            _overlayManager.AddOverlay(_overlay);
    }

    public void RemoveOverlay()
    {
        if (_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnGrainCvarChanged(bool enabled)
    {
        if (enabled)
        {
            _overlayManager.AddOverlay(_overlay);
        }
        else
        {
            _overlayManager.RemoveOverlay(_overlay);
        }
    }

    #endregion
}