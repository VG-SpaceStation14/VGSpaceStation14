using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public enum PdaVisuals
    {
        IdCardInserted,
        PdaType,
        ScreenOverlay
    }

    [Serializable, NetSerializable]
    public enum PdaUiKey
    {
        Key
    }
}