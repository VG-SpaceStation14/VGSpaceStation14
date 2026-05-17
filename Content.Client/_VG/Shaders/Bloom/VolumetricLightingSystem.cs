using Content.Shared._VG;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._VG.Shaders.Bloom;

public sealed partial class VolumetricLightSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;

    private VolumetricLightOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        Subs.CVar(_cfg, VGCCVars.BloomEnabled, OnBloomEnabledChanged);

        _overlay = new();
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (_cfg.GetCVar(VGCCVars.BloomEnabled))
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnBloomEnabledChanged(bool enabled)
    {
        if (enabled && _playerMan.LocalEntity != null)
            _overlayMan.AddOverlay(_overlay);
        else
            _overlayMan.RemoveOverlay(_overlay);
    }
}