using System.Text.Json.Serialization;

namespace Content.Server._VG.Sponsors;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "actionType")]
[JsonDerivedType(typeof(AddSponsorAction), "add")]
[JsonDerivedType(typeof(RemoveSponsorAction), "remove")]
[JsonDerivedType(typeof(AddLoadoutAction), "addloadout")]
[JsonDerivedType(typeof(RemoveLoadoutAction), "removeloadout")]
public abstract class PendingSponsorAction
{
    public string Username { get; set; } = string.Empty;
}

public sealed class AddSponsorAction : PendingSponsorAction
{
    public int Tier { get; set; }
    public DateTime? ExpireDate { get; set; }
    public string? Notes { get; set; }
    public string? OOCColor { get; set; }
}

public sealed class RemoveSponsorAction : PendingSponsorAction
{
}

public sealed class AddLoadoutAction : PendingSponsorAction
{
    public string LoadoutId { get; set; } = string.Empty;
}

public sealed class RemoveLoadoutAction : PendingSponsorAction
{
    public string LoadoutId { get; set; } = string.Empty;
}