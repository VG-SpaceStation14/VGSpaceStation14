using System.Diagnostics.CodeAnalysis;
using Content.Shared._VG.Sponsors;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Разрешает выбирать лодаут только спонсорам с опциональным ограничением по Tier.
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
    [DataField("tier")]
    public int? RequiredTier { get; private set; }

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var net = collection.Resolve<INetManager>();
        //VG-Tweak - Start
        if (net.IsServer)
            return true;
        //VG-Tweak - End

        if (session == null)
            return true;

        SponsorInfo? info = null;
        var isSponsor = false; //VG-Tweak

        if (collection.TryResolveType<ISponsorsManager>(out var sponsorsClient))
            isSponsor = sponsorsClient.TryGetInfo(out info) && info != null;

        if (!isSponsor || info == null)
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
            return false;
        }

        if (RequiredTier.HasValue)
        {
            var userTier = info.Tier ?? 0;
            if (userTier < RequiredTier.Value)
            {
                reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString(
                    "loadout-sponsor-tier-restriction",
                    ("requiredTier", RequiredTier.Value),
                    ("userTier", userTier)));
                return false;
            }
        }

        return true;
    }
}