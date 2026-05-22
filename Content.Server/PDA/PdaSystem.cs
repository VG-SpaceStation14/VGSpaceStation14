using Content.Server.Access.Systems;
using Content.Server.AlertLevel;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Managers;
using Content.Server.Instruments;
using Content.Server.PDA.Ringer;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Chat;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Implants;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Light;
using Content.Shared.Light.EntitySystems;
using Content.Shared.PDA;
using Content.Shared.PDA.Ringer;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.Preferences;
using Content.Server.Preferences;
using Content.Server.Preferences.Managers;
using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;

namespace Content.Server.PDA
{
    public sealed class PdaSystem : SharedPdaSystem
    {
        [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
        [Dependency] private readonly InstrumentSystem _instrument = default!;
        [Dependency] private readonly RingerSystem _ringer = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly StoreSystem _store = default!;
        [Dependency] private readonly IChatManager _chatManager = default!;
        [Dependency] private readonly UserInterfaceSystem _ui = default!;
        [Dependency] private readonly UnpoweredFlashlightSystem _unpoweredFlashlight = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly IdCardSystem _idCard = default!;
        // VG-Tweak Start
        [Dependency] private readonly IServerPreferencesManager _prefsManager = default!;
        // VG-Tweak End

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PdaComponent, LightToggleEvent>(OnLightToggle);

            // UI Events:
            SubscribeLocalEvent<PdaComponent, BoundUIOpenedEvent>(OnPdaOpen);
            SubscribeLocalEvent<PdaComponent, PdaRequestUpdateInterfaceMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaToggleFlashlightMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowRingtoneMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowMusicMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaShowUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaLockUplinkMessage>(OnUiMessage);
            SubscribeLocalEvent<PdaComponent, PdaSetWallpaperColorMessage>(OnUiMessage);
            // VG-Boot
            SubscribeLocalEvent<PdaComponent, PdaBootFinishedMessage>(OnUiMessage);

            SubscribeLocalEvent<PdaComponent, CartridgeLoaderNotificationSentEvent>(OnNotification);

            SubscribeLocalEvent<PdaComponent, MapInitEvent>(OnPdaMapInit); // VG-Tweak

            SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
            SubscribeLocalEvent<EntityRenamedEvent>(OnEntityRenamed, after: new[] { typeof(IdCardSystem) });
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<PdaComponent, InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent>>(ChameleonControllerOutfitItemSelected);
            SubscribeLocalEvent<PdaComponent, PdaSetWallpaperMessage>(OnUiMessage);

            // VG-Power Start
            SubscribeLocalEvent<PdaComponent, PdaTogglePowerMessage>(OnTogglePower);
            SubscribeLocalEvent<PdaComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PdaComponent, CartridgeLoaderActiveCartridgeChangedEvent>(OnActiveCartridgeChanged);
            // VG-Power End
        }

        // VG-Tweak Start
        private void OnPdaMapInit(EntityUid uid, PdaComponent pda, MapInitEvent args)
        {
            // Запускаем таймер с небольшой задержкой, чтобы контейнер точно был готов
            Timer.Spawn(TimeSpan.FromMilliseconds(500), () =>
            {
                // Проверяем, что PDA всё ещё существует и не удалено
                if (!EntityManager.EntityExists(uid))
                    return;

                // Ищем контейнер, в котором лежит PDA (обычно слот игрока)
                if (!_containerSystem.TryGetContainingContainer(uid, out var container))
                    return;

                if (!TryComp(container.Owner, out ActorComponent? actor))
                    return;

                var prefs = _prefsManager.GetPreferences(actor.PlayerSession.UserId);
                var selectedIndex = prefs.SelectedCharacterIndex;
                if (prefs.Characters.TryGetValue(selectedIndex, out var profile) && profile is HumanoidCharacterProfile humanoid)
                {
                    if (!string.IsNullOrEmpty(humanoid.PdaWallpaperPath))
                    {
                        pda.WallpaperPath = humanoid.PdaWallpaperPath;
                        Dirty(uid, pda);
                        UpdatePdaUi(uid, pda);
                    }
                }
            });
        }
        // VG-Tweak End

        private void ChameleonControllerOutfitItemSelected(Entity<PdaComponent> ent, ref InventoryRelayedEvent<ChameleonControllerOutfitSelectedEvent> args)
        {
            if (ent.Comp.ContainedId != null)
                RaiseLocalEvent(ent.Comp.ContainedId.Value, args);
        }

        private void OnEntityRenamed(ref EntityRenamedEvent ev)
        {
            if (HasComp<IdCardComponent>(ev.Uid))
                return;

            if (_idCard.TryFindIdCard(ev.Uid, out var idCard))
            {
                var query = EntityQueryEnumerator<PdaComponent>();

                while (query.MoveNext(out var uid, out var comp))
                {
                    if (comp.ContainedId == idCard)
                    {
                        SetOwner(uid, comp, ev.Uid, ev.NewName);
                    }
                }
            }
        }

        protected override void OnComponentInit(EntityUid uid, PdaComponent pda, ComponentInit args)
        {
            base.OnComponentInit(uid, pda, args);

            // VG-Power Start: начальное состояние – выключен
            pda.Powered = false;
            pda.Booted = false;
            pda.ScreenOverlay = "off";
            Dirty(uid, pda);
            UpdatePdaAppearance(uid, pda);
            // VG-Power End

            if (!HasComp<UserInterfaceComponent>(uid))
                return;

            UpdateAlertLevel(uid, pda);
            UpdateStationName(uid, pda);
        }

        protected override void OnItemInserted(EntityUid uid, PdaComponent pda, EntInsertedIntoContainerMessage args)
        {
            base.OnItemInserted(uid, pda, args);
            var id = CompOrNull<IdCardComponent>(pda.ContainedId);
            if (id != null)
                pda.OwnerName = id.FullName;
            UpdatePdaUi(uid, pda);
        }

        protected override void OnItemRemoved(EntityUid uid, PdaComponent pda, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID != pda.IdSlot.ID && args.Container.ID != pda.PenSlot.ID && args.Container.ID != pda.PaiSlot.ID)
                return;

            if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
                return;

            base.OnItemRemoved(uid, pda, args);
            UpdatePdaUi(uid, pda);
        }

        private void OnLightToggle(EntityUid uid, PdaComponent pda, LightToggleEvent args)
        {
            pda.FlashlightOn = args.IsOn;
            UpdatePdaUi(uid, pda);
        }

        public void SetOwner(EntityUid uid, PdaComponent pda, EntityUid owner, string ownerName)
        {
            pda.OwnerName = ownerName;
            pda.PdaOwner = owner;
            UpdatePdaUi(uid, pda);
        }

        private void OnStationRenamed(StationRenamedEvent ev)
        {
            UpdateAllPdaUisOnStation();
        }

        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            UpdateAllPdaUisOnStation();
        }

        private void UpdateAllPdaUisOnStation()
        {
            var query = EntityQueryEnumerator<PdaComponent>();
            while (query.MoveNext(out var ent, out var comp))
            {
                UpdatePdaUi(ent, comp);
            }
        }

        private void OnNotification(Entity<PdaComponent> ent, ref CartridgeLoaderNotificationSentEvent args)
        {
            _ringer.RingerPlayRingtone(ent.Owner);

            if (!_containerSystem.TryGetContainingContainer((ent, null, null), out var container)
                || !TryComp<ActorComponent>(container.Owner, out var actor))
                return;

            var message = FormattedMessage.EscapeText(args.Message);
            var wrappedMessage = Loc.GetString("pda-notification-message",
                ("header", args.Header),
                ("message", message));

            _chatManager.ChatMessageToOne(
                ChatChannel.Notifications,
                message,
                wrappedMessage,
                EntityUid.Invalid,
                false,
                actor.PlayerSession.Channel);
        }

        public override void UpdatePdaUi(EntityUid uid, PdaComponent? pda = null)
        {
            if (!Resolve(uid, ref pda, false))
                return;

            if (!_ui.HasUi(uid, PdaUiKey.Key))
                return;

            var address = GetDeviceNetAddress(uid);
            var hasInstrument = HasComp<InstrumentComponent>(uid);
            var showUplink = HasComp<UplinkComponent>(uid) && IsUnlocked(uid);

            UpdateStationName(uid, pda);
            UpdateAlertLevel(uid, pda);

            if (!TryComp(uid, out CartridgeLoaderComponent? loader))
                return;

            var programs = _cartridgeLoader.GetAvailablePrograms(uid, loader);
            var id = CompOrNull<IdCardComponent>(pda.ContainedId);
            var state = new PdaUpdateState(
                programs,
                GetNetEntity(loader.ActiveProgram),
                pda.FlashlightOn,
                pda.PenSlot.HasItem,
                pda.PaiSlot.HasItem,
                new PdaIdInfoText
                {
                    ActualOwnerName = pda.OwnerName,
                    IdOwner = id?.FullName,
                    JobTitle = id?.LocalizedJobTitle,
                    StationAlertLevel = pda.StationAlertLevel,
                    StationAlertColor = pda.StationAlertColor
                },
                pda.StationName,
                showUplink,
                hasInstrument,
                address,
                pda.HasWallpaperColor,
                pda.WallpaperColor,
                pda.Booted, // VG-Boot
                pda.WallpaperPath, // VG-Wallpaper
                pda.Powered, // VG-Power
                pda.ScreenOverlay); // VG-Power

            _ui.SetUiState(uid, PdaUiKey.Key, state);
        }

        private void OnPdaOpen(Entity<PdaComponent> ent, ref BoundUIOpenedEvent args)
        {
            if (!PdaUiKey.Key.Equals(args.UiKey))
                return;

            // VG-Power Start: при открытии включаем, если выключен, и уведомляем картридж
            bool wasPowered = ent.Comp.Powered;

            if (!ent.Comp.Powered)
            {
                ent.Comp.Powered = true;
                UpdateOverlayFromPoweredAndCartridge(ent.Owner, ent.Comp);
                Dirty(ent.Owner, ent.Comp);
                UpdatePdaAppearance(ent.Owner, ent.Comp);
                UpdatePdaUi(ent.Owner, ent.Comp);
            }
            else
            {
                UpdateOverlayFromPoweredAndCartridge(ent.Owner, ent.Comp);
                Dirty(ent.Owner, ent.Comp);
                UpdatePdaAppearance(ent.Owner, ent.Comp);
                UpdatePdaUi(ent.Owner, ent.Comp);
            }

            // Если ПДА был выключен и включился – уведомляем активный картридж об обновлении UI
            if (!wasPowered && ent.Comp.Powered)
            {
                if (TryComp(ent.Owner, out CartridgeLoaderComponent? loader) && loader.ActiveProgram != null)
                {
                    RaiseLocalEvent(loader.ActiveProgram.Value, new CartridgeUiReadyEvent(ent.Owner));
                }
            }
            // VG-Power End
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaRequestUpdateInterfaceMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            UpdatePdaUi(uid, pda);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaSetWallpaperMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            pda.WallpaperPath = msg.Path;
            Dirty(uid, pda);
            UpdatePdaUi(uid, pda);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaToggleFlashlightMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            _unpoweredFlashlight.TryToggleLight(uid, user: null);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowRingtoneMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (HasComp<RingerComponent>(uid))
                _ringer.TryToggleRingerUi(uid, msg.Actor);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowMusicMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<InstrumentComponent>(uid, out var instrument))
                _instrument.ToggleInstrumentUi(uid, msg.Actor, instrument);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaShowUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (HasComp<UplinkComponent>(uid) && IsUnlocked(uid))
                _store.ToggleUi(msg.Actor, uid);
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaLockUplinkMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (TryComp<RingerUplinkComponent>(uid, out var uplink))
            {
                _ringer.LockUplink((uid, uplink));
                UpdatePdaUi(uid, pda);
            }
        }

        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaSetWallpaperColorMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            pda.WallpaperColor = msg.Color.WithAlpha(1f);
            pda.HasWallpaperColor = true;
            UpdatePdaUi(uid, pda);
        }

        // VG-Boot
        private void OnUiMessage(EntityUid uid, PdaComponent pda, PdaBootFinishedMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            if (pda.Booted)
                return;

            pda.Booted = true;
            Dirty(uid, pda);

            _ringer.RingerPlayRingtone(uid);

            UpdatePdaUi(uid, pda);
        }

        // VG-Power Start
        private void UpdateOverlayFromPoweredAndCartridge(EntityUid uid, PdaComponent pda)
        {
            if (!pda.Powered)
            {
                pda.ScreenOverlay = "off";
                return;
            }

            if (TryComp(uid, out CartridgeLoaderComponent? loader) && loader.ActiveProgram != null &&
                TryComp(loader.ActiveProgram, out CartridgeComponent? cart) && !string.IsNullOrEmpty(cart.ScreenOverlay))
            {
                pda.ScreenOverlay = cart.ScreenOverlay;
            }
            else
            {
                pda.ScreenOverlay = "on";
            }
        }

        private void OnTogglePower(EntityUid uid, PdaComponent pda, PdaTogglePowerMessage msg)
        {
            if (!PdaUiKey.Key.Equals(msg.UiKey))
                return;

            pda.Powered = !pda.Powered;
            UpdateOverlayFromPoweredAndCartridge(uid, pda);
            Dirty(uid, pda);
            UpdatePdaAppearance(uid, pda);
            UpdatePdaUi(uid, pda);

            if (!pda.Powered && TryComp<ActorComponent>(msg.Actor, out var actor))
                _ui.CloseUi(uid, PdaUiKey.Key, actor.PlayerSession);
        }

        private void OnUseInHand(EntityUid uid, PdaComponent pda, UseInHandEvent args)
        {
            // Пусто – ActivatableUI сам откроет окно
        }

        private void OnActiveCartridgeChanged(EntityUid uid, PdaComponent pda, CartridgeLoaderActiveCartridgeChangedEvent args)
        {
            if (!pda.Powered)
                return;

            UpdateOverlayFromPoweredAndCartridge(uid, pda);
            Dirty(uid, pda);
            UpdatePdaAppearance(uid, pda);
            UpdatePdaUi(uid, pda);
        }
        // VG-Power End

        private bool IsUnlocked(EntityUid uid)
        {
            return !TryComp<RingerUplinkComponent>(uid, out var uplink) || uplink.Unlocked;
        }

        private void UpdateStationName(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            pda.StationName = station is null ? null : Name(station.Value);
        }

        private void UpdateAlertLevel(EntityUid uid, PdaComponent pda)
        {
            var station = _station.GetOwningStation(uid);
            if (!TryComp(station, out AlertLevelComponent? alertComp) ||
                alertComp.AlertLevels == null)
                return;
            pda.StationAlertLevel = alertComp.CurrentLevel;
            if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
                pda.StationAlertColor = details.Color;
        }

        private string? GetDeviceNetAddress(EntityUid uid)
        {
            string? address = null;

            if (TryComp(uid, out DeviceNetworkComponent? deviceNetworkComponent))
            {
                address = deviceNetworkComponent?.Address;
            }

            return address;
        }

        private void UpdatePdaAppearance(EntityUid uid, PdaComponent pda)
        {
            Appearance.SetData(uid, PdaVisuals.IdCardInserted, pda.ContainedId != null);
            if (!string.IsNullOrEmpty(pda.ScreenOverlay))
                Appearance.SetData(uid, PdaVisuals.ScreenOverlay, pda.ScreenOverlay);
            else
                Appearance.SetData(uid, PdaVisuals.ScreenOverlay, "off");
        }
    }
}