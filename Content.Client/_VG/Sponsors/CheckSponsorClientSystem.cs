using System.Linq;
using Content.Shared._VG.Sponsors;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._VG.Sponsors
{
    public sealed class CheckSponsorClientSystem : EntitySystem
    {
        public bool IsSponsor = false;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CheckedUserSponsor>(SetSponsorStatus);
        }

        private void SetSponsorStatus(CheckedUserSponsor ev)
        {
            IsSponsor = ev.IsSponsor;
        }

        public void TryCheckSponsor(string? player)
        {
            if (player != null)
            {
                var ev = new CheckUserSponsor(player);
                RaiseNetworkEvent(ev);
            }
        }

        public bool GetSponsorStatus()
        {
            return IsSponsor;
        }

    }
}
