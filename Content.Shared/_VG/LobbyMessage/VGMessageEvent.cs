using System;
using Robust.Shared.Serialization;

namespace Content.Shared._VG.LobbyMessage;

[Serializable, NetSerializable]
public sealed class VGMessageEvent : EntityEventArgs
{
    public string Text;

    public VGMessageEvent(string text)
    {
        Text = text;
    }
}