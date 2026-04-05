using System.Diagnostics.CodeAnalysis;
using Content.Shared._VG.Sponsors;
using Robust.Shared.Network;

namespace Content.Client._VG.Sponsors;

public sealed class SponsorsManager : ISponsorsManager
{
    [Dependency] private readonly IClientNetManager _netMgr = default!;

    private SponsorInfo? _info;

    public void Initialize()
    {
        _netMgr.RegisterNetMessage<MsgSponsorInfo>(msg => _info = msg.Info);
    }

    public bool TryGetInfo([NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        sponsor = _info;
        return _info != null;
    }

    bool ISponsorsManager.TryGetInfo(NetUserId userId, [NotNullWhen(true)] out SponsorInfo? sponsor)
    {
        sponsor = null;
        return false;
    }
}