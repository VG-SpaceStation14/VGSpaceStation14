using Robust.Shared.GameObjects;

namespace Content.Shared.CartridgeLoader;

public sealed class CartridgeLoaderActiveCartridgeChangedEvent : EntityEventArgs
{
    public EntityUid? ActiveCartridge { get; }
    public CartridgeLoaderActiveCartridgeChangedEvent(EntityUid? activeCartridge)
    {
        ActiveCartridge = activeCartridge;
    }
}