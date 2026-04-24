using Content.Shared._VG.Implants.Components;
using Content.Shared._VG.Implants.Systems;
using Content.Shared.Damage;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._VG.Implants.Systems;

public sealed class CriticalSaveImplantSystem : SharedCriticalSaveImplantSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    private static readonly SoundSpecifier HeartbeatSound =
        new SoundPathSpecifier("/Audio/_VG/Effects/heartbeat.ogg",
            AudioParams.Default.WithVolume(-3f));

    public override void Update(float frameTime)
    {
        if (Net.IsClient)
            return;

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<CriticalSaveImplantComponent, SubdermalImplantComponent>();

        while (query.MoveNext(out var uid, out var comp, out var implant))
        {
            if (!comp.IsActive || comp.ExpireTime == null)
                continue;

            if (curTime >= comp.ExpireTime.Value)
            {
                if (implant.ImplantedEntity is { } target && comp.SavedDamage != null)
                {
                    Damageable.ChangeDamage((target, null), comp.SavedDamage, ignoreResistances: true);
                    Popup.PopupEntity(Loc.GetString("critical-save-implant-expired"), target, target, PopupType.MediumCaution);
                }

                ActivatedImplants.Remove(uid);
                QueueDel(uid);
                continue;
            }

            UpdateHeartbeatCooldown(comp, curTime);

            if (curTime >= comp.NextHeartbeatTime && implant.ImplantedEntity is { } heartbeatTarget)
            {
                PlayHeartbeat(heartbeatTarget, comp);
                comp.NextHeartbeatTime = curTime + TimeSpan.FromSeconds(comp.CurrentHeartbeatCooldown);
                Dirty(uid, comp);
            }
        }
    }

    private void PlayHeartbeat(EntityUid target, CriticalSaveImplantComponent comp)
    {
        var progressNormalized = (comp.CurrentHeartbeatCooldown - InitialHeartbeatCooldown) /
                                 (FinalHeartbeatCooldown - InitialHeartbeatCooldown);
        var pitch = 1.2f - (float)progressNormalized * 0.5f;

        if (_player.TryGetSessionByEntity(target, out var session))
            _audio.PlayGlobal(HeartbeatSound, session, AudioParams.Default.WithPitchScale(pitch));
        else
            _audio.PlayGlobal(HeartbeatSound, target, AudioParams.Default.WithPitchScale(pitch));
    }
}