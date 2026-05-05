using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Robust.Client.State;

namespace Content.Client._VG.Arrival;

public sealed class VGArrivalSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private bool _shown;
    private EntityUid? _lastEntity;
    private bool _wasInLobby;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _shown = false;
        _lastEntity = null;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var currentState = _stateManager.CurrentState;
        var isInLobby = currentState?.GetType().Name == "LobbyState";
        
        if (isInLobby)
        {
            if (!_wasInLobby)
            {
                _shown = false;
                _lastEntity = null;
            }
            _wasInLobby = true;
            return;
        }
        
        _wasInLobby = false;

        var session = _player.LocalSession;
        if (session?.AttachedEntity == null)
        {
            _lastEntity = null;
            return;
        }

        var currentEntity = session.AttachedEntity.Value;

        if (_lastEntity != null && _lastEntity != currentEntity)
        {
            _shown = false;
        }

        _lastEntity = currentEntity;

        if (_shown)
            return;

        if (HasComp<GhostComponent>(currentEntity))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(currentEntity))
            return;

        _shown = true;

        var overlay = new VGArrivalOverlay(currentEntity, session.Name);

        var screen = _ui.ActiveScreen;
        screen?.AddChild(overlay);
    }
}