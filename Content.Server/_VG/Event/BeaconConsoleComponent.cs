[RegisterComponent]
public sealed partial class BeaconConsoleComponent : Component
{
    [DataField] public string Password = "ADMIN";
    public int Attempts = 0;
    public bool IsEnabled = false;
    public bool IsLocked = false;
    public bool IsBeaconActive = false;
}