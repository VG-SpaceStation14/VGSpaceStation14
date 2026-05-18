using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._VG.LobbyMessage;

/// <summary>
/// Запрос от клиента на получение текущего сообщения лобби.
/// </summary>
[Serializable, NetSerializable]
public sealed class MsgVGMessageRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer) { }
    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer) { }
}