using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Serialization;

namespace Content.Shared._VG.Event;

[Serializable, NetSerializable]
public enum BeaconConsoleUiKey : byte { Key }

[Serializable, NetSerializable]
public sealed class BeaconConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsEnabled;
    public readonly bool IsLocked;
    public readonly bool IsBeaconActive;
    public readonly NavInterfaceState? NavState;
    
    public BeaconConsoleBoundUserInterfaceState(bool isEnabled, bool isLocked, bool isBeaconActive, NavInterfaceState? navState)
    {
        IsEnabled = isEnabled;
        IsLocked = isLocked;
        IsBeaconActive = isBeaconActive;
        NavState = navState;
    }
}

[Serializable, NetSerializable]
public sealed class BeaconConsoleAttemptMessage : BoundUserInterfaceMessage
{
    public readonly string Password;
    public BeaconConsoleAttemptMessage(string password) => Password = password;
}

[Serializable, NetSerializable]
public sealed class BeaconActivateMessage : BoundUserInterfaceMessage { }