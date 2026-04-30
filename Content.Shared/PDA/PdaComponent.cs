using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.PDA
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class PdaComponent : Component
    {
        public const string PdaIdSlotId = "PDA-id";
        public const string PdaPenSlotId = "PDA-pen";
        public const string PdaPaiSlotId = "PDA-pai";

        [DataField("idSlot")]
        public ItemSlot IdSlot = new();

        [DataField("penSlot")]
        public ItemSlot PenSlot = new();
        [DataField("paiSlot")]
        public ItemSlot PaiSlot = new();

        [DataField("id", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? IdCard;

        [ViewVariables] public EntityUid? ContainedId;
        [ViewVariables] public bool FlashlightOn;

        [ViewVariables(VVAccess.ReadWrite)] public string? OwnerName;
        [ViewVariables(VVAccess.ReadWrite)] public EntityUid? PdaOwner;
        [ViewVariables] public string? StationName;
        [ViewVariables] public string? StationAlertLevel;
        [ViewVariables] public Color StationAlertColor = Color.White;

        [DataField]
        public bool HasWallpaperColor;

        [DataField]
        public Color WallpaperColor = Color.White;

        // VG-Boot
        [DataField, AutoNetworkedField]
        public bool Booted;
    }
}