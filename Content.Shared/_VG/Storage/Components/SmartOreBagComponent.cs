using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared._VG.Storage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SmartOreBagComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<EntityPrototype>> IgnoredOres = new();
}

[Serializable, NetSerializable]
public sealed class OpenSmartOreBagWindowMessage : EntityEventArgs
{
    public NetEntity Entity; 
    public List<ProtoId<EntityPrototype>> IgnoredOres;

    public OpenSmartOreBagWindowMessage(NetEntity entity, List<ProtoId<EntityPrototype>> ignoredOres)
    {
        Entity = entity;
        IgnoredOres = ignoredOres;
    }
}

[Serializable, NetSerializable]
public sealed class SmartOreBagUpdateMessage : EntityEventArgs
{
    public NetEntity Entity;
    public List<ProtoId<EntityPrototype>> IgnoredOres;

    public SmartOreBagUpdateMessage(NetEntity entity, List<ProtoId<EntityPrototype>> ignoredOres)
    {
        Entity = entity;
        IgnoredOres = ignoredOres;
    }
}