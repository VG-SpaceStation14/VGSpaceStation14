using Content.Client._VG.Storage.UI;
using Content.Shared._VG.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Client._VG.Storage;

[UsedImplicitly]
public sealed class SmartOreBagUIController : EntitySystem
{
    private SmartOreBagWindow? _currentWindow;
    private NetEntity _currentEntity;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenSmartOreBagWindowMessage>(OnOpenWindow);
    }

    private void OnOpenWindow(OpenSmartOreBagWindowMessage msg)
    {
        _currentEntity = msg.Entity;

        _currentWindow = new SmartOreBagWindow();
        _currentWindow.UpdateState(msg.IgnoredOres);

        _currentWindow.OnConfirmed += (ignoredOres) =>
        {
            var updateMsg = new SmartOreBagUpdateMessage(_currentEntity, ignoredOres);
            RaiseNetworkEvent(updateMsg);
            
            _currentWindow = null; 
        };

        _currentWindow.OpenCentered();
    }
}