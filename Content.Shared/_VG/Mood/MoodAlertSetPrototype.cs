using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared._VG.Mood;

[Prototype("moodAlertSet")]
public sealed partial class MoodAlertSetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true, customTypeSerializer: typeof(DictionarySerializer<MoodThreshold, string>))]
    public Dictionary<MoodThreshold, string> AlertMapping = new();
}