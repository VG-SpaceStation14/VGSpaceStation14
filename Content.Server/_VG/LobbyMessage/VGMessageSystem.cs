using System.Linq;
using Content.Shared.GameTicking;
using Content.Shared._VG.LobbyMessage;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Server._VG.LobbyMessage;

public sealed class VGMessageSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier MessageSound = new SoundPathSpecifier("/Audio/Voice/Moth/moth_scream.ogg");

    private string? _currentMessage;

    public override void Initialize()
    {
        base.Initialize();
        _netManager.RegisterNetMessage<MsgVGMessageRequest>(OnRequest, NetMessageAccept.Server);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _currentMessage = null;
        var clearEv = new VGMessageEvent(string.Empty);
        foreach (var session in _playerManager.Sessions)
        {
            RaiseNetworkEvent(clearEv, session);
        }
    }

    public void SendMessage(string? text)
    {
        _currentMessage = string.IsNullOrWhiteSpace(text) ? null : text;

        var ev = new VGMessageEvent(text ?? string.Empty);
        foreach (var session in _playerManager.Sessions)
        {
            RaiseNetworkEvent(ev, session);
            if (!string.IsNullOrWhiteSpace(text))
                _audio.PlayGlobal(MessageSound, session);
        }
    }

    private void OnRequest(MsgVGMessageRequest message)
    {
        if (message.MsgChannel == null) return;
        var session = _playerManager.GetSessionByChannel(message.MsgChannel);
        if (session == null) return;

        var ev = new VGMessageEvent(_currentMessage ?? string.Empty);
        RaiseNetworkEvent(ev, session);
    }
}