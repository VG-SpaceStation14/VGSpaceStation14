using Content.Shared._VG.Event;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._VG.Event;

public sealed class BeaconConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BeaconConsoleWindow? _window;

    public BeaconConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<BeaconConsoleWindow>();
        
        _window.OnAttemptAuth += pass => SendMessage(new BeaconConsoleAttemptMessage(pass));
        _window.OnActivatePressed += () => SendMessage(new BeaconActivateMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BeaconConsoleBoundUserInterfaceState beaconState)
        {
            _window?.UpdateState(beaconState, beaconState.NavState);
        }
    }
}