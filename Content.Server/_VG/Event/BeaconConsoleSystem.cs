using Content.Shared._VG.Event;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Content.Server.Chat.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;

namespace Content.Server._VG.Event;

public sealed class BeaconConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeaconConsoleComponent, BeaconConsoleAttemptMessage>(OnAuthAttempt);
        SubscribeLocalEvent<BeaconConsoleComponent, BeaconActivateMessage>(OnActivateBeacon);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BeaconConsoleComponent, RadarConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var beacon, out var radar, out var ui))
        {
            if (!beacon.IsEnabled || !beacon.IsBeaconActive || !_ui.IsUiOpen(uid, BeaconConsoleUiKey.Key))
                continue;

            UpdateUI(uid, beacon, radar);
        }
    }

    private void UpdateUI(EntityUid uid, BeaconConsoleComponent component, RadarConsoleComponent radar)
    {
        var xform = Transform(uid);

        var entityCoords = xform.Coordinates;
        var netCoords = GetNetCoordinates(entityCoords);
        
        var angle = xform.WorldRotation;

        var docks = new Dictionary<NetEntity, List<DockingPortState>>();

        var navState = new NavInterfaceState(radar.MaxRange, netCoords, angle, docks);

        _ui.SetUiState(uid, BeaconConsoleUiKey.Key, 
            new BeaconConsoleBoundUserInterfaceState(
                component.IsEnabled, 
                component.IsLocked, 
                component.IsBeaconActive, 
                navState));
    }

    private void OnAuthAttempt(EntityUid uid, BeaconConsoleComponent component, BeaconConsoleAttemptMessage args)
    {
        if (component.IsLocked) return;

        if (args.Password == component.Password)
        {
            component.IsEnabled = true;
            component.Attempts = 0;
        }
        else
        {
            component.Attempts++;
            if (component.Attempts >= 3)
            {
                component.IsLocked = true;
                _chat.DispatchGlobalAnnouncement(
                    "ВНИМАНИЕ! Несанкционированная попытка доступа к терминалу управления маяком.",
                    "Служба Безопасности",
                    playSound: true, 
                    colorOverride: Color.Red);
            }
        }
        SyncUI(uid, component);
    }

    private void OnActivateBeacon(EntityUid uid, BeaconConsoleComponent component, BeaconActivateMessage args)
    {
        if (!component.IsEnabled || component.IsBeaconActive) return;

        component.IsBeaconActive = true;
        _chat.DispatchGlobalAnnouncement(
            "Маяк успешно активирован. Масс-сканер откалиброван и готов к работе.",
            "Система Маяка",
            playSound: true, 
            colorOverride: Color.SeaBlue);

        SyncUI(uid, component);
    }

    private void SyncUI(EntityUid uid, BeaconConsoleComponent component)
    {
        if (TryComp<RadarConsoleComponent>(uid, out var radar))
            UpdateUI(uid, component, radar);
    }
}