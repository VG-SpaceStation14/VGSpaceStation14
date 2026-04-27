using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Utility;
using Content.Client.ADTUserInterface.RichText;

namespace Content.Client.ADT.UserInterface.RichText;

public sealed class EntityTextureTag : BaseTextureTag, IMarkupTagHandler
{
    public string Name => "enttex";

    public bool TryCreateControl(MarkupNode node, [NotNullWhen(true)] out Control? control)
    {
        control = null;

        if (!node.Attributes.TryGetValue("id", out var idParameter) || !TryGetLong(idParameter, out var id))
            return false;

        if (!node.Attributes.TryGetValue("size", out var size) || !TryGetLong(size, out var sizeValue))
        {
            sizeValue = 32;
        }

        if (!node.Attributes.TryGetValue("scale", out var scale) || !TryGetLong(scale, out var scaleValue))
        {
            scaleValue = 2;
        }

        if (!node.Attributes.TryGetValue("offsetX", out var xParameter) || !TryGetLong(xParameter, out var x))
            x = 0;

        if (!node.Attributes.TryGetValue("offsetY", out var yParameter) || !TryGetLong(yParameter, out var y))
            y = 0;

        if (!TryDrawIconEntity(new NetEntity((int) id), sizeValue.Value, scaleValue.Value, new Vector2((float) x, (float) y), out var texture))
            return false;

        control = texture;

        return true;
    }

    private static bool TryGetLong(MarkupParameter parameter, [NotNullWhen(true)] out long? value)
    {
        if (parameter.TryGetLong(out value))
            return true;

        if (parameter.TryGetString(out var stringValue) && long.TryParse(stringValue, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }
}
