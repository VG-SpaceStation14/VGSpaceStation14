using Robust.Shared.Serialization;

namespace Content.Shared.PDA
{
    [Serializable, NetSerializable]
    public enum PdaVisualLayers : byte
    {
        Base,
        Flashlight,
        IdLight,
        ScreenOverlay
    }
}