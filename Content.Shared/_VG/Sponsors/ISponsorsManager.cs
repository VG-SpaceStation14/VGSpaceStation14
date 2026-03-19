using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Network;

namespace Content.Shared._VG.Sponsors
{
    public interface ISponsorsManager
    {
        bool TryGetInfo([NotNullWhen(true)] out SponsorInfo? info);
        bool TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? info);
    }
}
