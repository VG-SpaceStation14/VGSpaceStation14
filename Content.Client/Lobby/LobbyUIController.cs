using System.Linq;
using Content.Client.Guidebook;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Client.Lobby.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Station;
using Content.Shared.ADT.Language;
using Content.Shared.CCVar;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Traits;
using Content.Shared._VG; // VG-Tweak
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using VGLobby = Content.Client._VG.Lobby.UI; // VG-Tweak
using Content.Client._VG.Lobby.UI;

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IFileDialogManager _dialogManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly JobRequirementsManager _requirements = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [UISystemDependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [UISystemDependency] private readonly ClientInventorySystem _inventory = default!;
    [UISystemDependency] private readonly StationSpawningSystem _spawn = default!;
    [UISystemDependency] private readonly GuidebookSystem _guide = default!;

    private CharacterSetupGui? _characterSetup;
    private HumanoidProfileEditor? _profileEditor;
    private CharacterSetupGuiSavePanel? _savePanel;
    private VGLobby.VGCharacterSetupWindow? _setupWindow; // VG-Tweak
    private bool _savePanelRequested; // VG-Tweak
    private int? _pendingSelectSlot; // VG-Tweak

    private LobbyCharacterPreviewPanel? PreviewPanel => GetLobbyPreview();
    private HumanoidCharacterProfile? EditedProfile => _profileEditor?.Profile;
    private int? EditedSlot => _profileEditor?.CharacterSlot;

    public override void Initialize()
    {
        base.Initialize();
        _prototypeManager.PrototypesReloaded += OnProtoReload;
        _preferencesManager.OnServerDataLoaded += PreferencesDataLoaded;
        _requirements.Updated += OnRequirementsUpdated;

        _configurationManager.OnValueChanged(CCVars.FlavorText, args =>
        {
            _profileEditor?.RefreshFlavorText();
        });

        _configurationManager.OnValueChanged(CCVars.GameRoleTimers, _ => RefreshProfileEditor());
        _configurationManager.OnValueChanged(CCVars.GameRoleLoadoutTimers, _ => RefreshProfileEditor());
        _configurationManager.OnValueChanged(CCVars.GameRoleWhitelist, _ => RefreshProfileEditor());
    }

    private LobbyCharacterPreviewPanel? GetLobbyPreview()
    {
        if (_stateManager.CurrentState is LobbyState lobby)
        {
            return lobby.Lobby?.CharacterPreview;
        }

        return null;
    }

    private void OnRequirementsUpdated()
    {
        if (_profileEditor != null)
        {
            _profileEditor.RefreshAntags();
            _profileEditor.RefreshJobs();
        }
    }

    private void OnProtoReload(PrototypesReloadedEventArgs obj)
    {
        if (_profileEditor != null)
        {
            if (obj.WasModified<AntagPrototype>())
            {
                _profileEditor.RefreshAntags();
            }

            if (obj.WasModified<JobPrototype>() ||
                obj.WasModified<DepartmentPrototype>())
            {
                _profileEditor.RefreshJobs();
            }

            if (obj.WasModified<LoadoutPrototype>() ||
                obj.WasModified<LoadoutGroupPrototype>() ||
                obj.WasModified<RoleLoadoutPrototype>())
            {
                _profileEditor.RefreshLoadouts();
            }

            if (obj.WasModified<SpeciesPrototype>())
            {
                _profileEditor.RefreshSpecies();
            }

            if (obj.WasModified<TraitPrototype>())
            {
                _profileEditor.RefreshTraits();
            }

            if (obj.WasModified<LanguagePrototype>())
            {
                _profileEditor.RefreshLanguages();
            }
        }
    }

    private void PreferencesDataLoaded()
    {
        PreviewPanel?.SetLoaded(true);
        PreviewPanel?.UpdateCharacterSelector();

        // VG-Tweak Start
        if (_setupWindow is { IsOpen: true } && _setupWindow.Contents.GetChild(0) is VGLobby.VGCharacterSetupWindowGui gui)
        {
            gui.UpdateProfileEditor();
            gui.UpdatePreview();
            gui.UpdateNameField();
            gui.ReloadCharacterPickers();
        }
        // VG-Tweak End

        if (_stateManager.CurrentState is not LobbyState) return;
        ReloadCharacterSetup();
    }

    public void OnStateEntered(LobbyState state)
    {
        PreviewPanel?.SetLoaded(_preferencesManager.ServerDataLoaded);
        ReloadCharacterSetup();
    }

    public void OnStateExited(LobbyState state)
    {
        PreviewPanel?.SetLoaded(false);
        CleanupNewWindow(); // VG-Tweak
        CloseCharacterSetupOld();
        _profileEditor?.Dispose();
        _characterSetup?.Dispose();
        _characterSetup = null;
        _profileEditor = null;
    }

    public void ReloadCharacterSetup()
    {
        RefreshLobbyPreview();
        if (_profileEditor != null)
        {
            _profileEditor.SetProfile(
                (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter,
                _preferencesManager.Preferences?.SelectedCharacterIndex);
        }
        _characterSetup?.ReloadCharacterPickers();

        // VG-Tweak Start: Обновление открытого окна при переключении извне
        if (_setupWindow is { IsOpen: true } && _setupWindow.Contents.GetChild(0) is VGLobby.VGCharacterSetupWindowGui gui)
        {
            gui.UpdateProfileEditor();
            gui.UpdatePreview();
            gui.UpdateNameField();
            gui.ReloadCharacterPickers();
        }
        // VG-Tweak End
    }

    private void RefreshLobbyPreview()
    {
        if (PreviewPanel == null)
            return;

        var character = _preferencesManager.Preferences?.SelectedCharacter;

        if (character is not HumanoidCharacterProfile humanoid)
        {
            PreviewPanel.SetSprite(EntityUid.Invalid);
            PreviewPanel.SetSummaryText(string.Empty);
            return;
        }

        var dummy = LoadProfileEntity(humanoid, null, true);
        PreviewPanel.SetSprite(dummy);
        PreviewPanel.SetSummaryText(humanoid.Summary);

        PreviewPanel.UpdateCharacterSelector();
    }

    private void RefreshProfileEditor()
    {
        _profileEditor?.RefreshAntags();
        _profileEditor?.RefreshJobs();
        _profileEditor?.RefreshLoadouts();
    }

    private void SaveProfile()
    {
        DebugTools.Assert(EditedProfile != null);

        if (EditedProfile == null || EditedSlot == null)
            return;

        var selected = _preferencesManager.Preferences?.SelectedCharacterIndex;

        if (selected == null)
            return;

        _preferencesManager.UpdateCharacter(EditedProfile, EditedSlot.Value);
        ReloadCharacterSetup();
    }

    // VG-Tweak Start
    private void SaveProfileNew(HumanoidProfileEditor editor)
    {
        if (editor.Profile == null || editor.CharacterSlot == null)
            return;

        _preferencesManager.UpdateCharacter(editor.Profile, editor.CharacterSlot.Value);
        editor.IsDirty = false;
        ReloadCharacterSetup();
    }
    // VG-Tweak End

    // VG-Tweak Start
    public void OpenCharacterSetup()
    {
        bool newWindowEnabled = _configurationManager.GetCVar(VGCCVars.CharacterSetupNewWindowEnabled);
        if (newWindowEnabled)
            OpenCharacterSetupNewWindow();
        else
            OpenCharacterSetupOld();
    }
    // VG-Tweak End

    private void OpenCharacterSetupOld()
    {
        if (_stateManager.CurrentState is LobbyState lobby)
        {
            EnsureOldGui();
            lobby.SwitchState(LobbyGui.LobbyGuiState.CharacterSetup);
        }
    }

    public void CloseCharacterSetupOld()
    {
        if (_characterSetup != null)
        {
            _characterSetup.Orphan();
            _characterSetup.Dispose();
            _characterSetup = null;
        }
        _profileEditor?.Dispose();
        _profileEditor = null;
    }

    private void EnsureOldGui()
    {
        if (_characterSetup != null && _profileEditor != null)
            return;

        _profileEditor = new HumanoidProfileEditor(
            _preferencesManager,
            _configurationManager,
            EntityManager,
            _dialogManager,
            LogManager,
            _playerManager,
            _prototypeManager,
            _resourceCache,
            _requirements,
            _markings,
            _factory);

        _profileEditor.OnOpenGuidebook += _guide.OpenHelp;

        _characterSetup = new CharacterSetupGui(_profileEditor);

        _characterSetup.CloseButton.OnPressed += _ =>
        {
            if (_profileEditor.Profile != null && _profileEditor.IsDirty)
                OpenSavePanelForOldMode();
            else
                CloseCharacterSetupOldAndReturn();
        };

        _profileEditor.Save += () =>
        {
            SaveProfile();
        };

        _characterSetup.SelectCharacter += args =>
        {
            _preferencesManager.SelectCharacter(args);
            ReloadCharacterSetup();
            PreviewPanel?.UpdateCharacterSelector();
        };

        _characterSetup.DeleteCharacter += args =>
        {
            _preferencesManager.DeleteCharacter(args);

            if (EditedSlot == args)
            {
                ReloadCharacterSetup();
            }
            else
            {
                _characterSetup?.ReloadCharacterPickers();
            }

            PreviewPanel?.UpdateCharacterSelector();
        };

        if (_stateManager.CurrentState is LobbyState lobby)
        {
            lobby.Lobby?.CharacterSetupState.AddChild(_characterSetup);
        }

        _profileEditor.SetProfile(
            (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter,
            _preferencesManager.Preferences?.SelectedCharacterIndex);

        _characterSetup.ReloadCharacterPickers();
    }

    private void CloseCharacterSetupOldAndReturn()
    {
        if (_stateManager.CurrentState is LobbyState lobby)
        {
            lobby.SwitchState(LobbyGui.LobbyGuiState.Default);
        }
    }

    private void OpenSavePanelForOldMode()
    {
        if (_savePanel is { IsOpen: true })
            return;

        _savePanel = new CharacterSetupGuiSavePanel();

        _savePanel.SaveButton.OnPressed += _ =>
        {
            SaveProfile();
            _savePanel.Close();
            CloseCharacterSetupOldAndReturn();
        };

        _savePanel.NoSaveButton.OnPressed += _ =>
        {
            _savePanel.Close();
            CloseCharacterSetupOldAndReturn();
        };

        _savePanel.CancelButton.OnPressed += _ =>
        {
            _savePanel.Close();
        };

        _savePanel.OpenCentered();
    }

    // VG-Tweak Start
    public void OpenCharacterSetupNewWindow()
    {
        _savePanelRequested = false;

        if (_setupWindow != null && _setupWindow.IsOpen)
        {
            _setupWindow.MoveToFront();
            return;
        }

        var profileEditor = new HumanoidProfileEditor(
            _preferencesManager,
            _configurationManager,
            EntityManager,
            _dialogManager,
            LogManager,
            _playerManager,
            _prototypeManager,
            _resourceCache,
            _requirements,
            _markings,
            _factory);

        profileEditor.OnOpenGuidebook += _guide.OpenHelp;

        var windowGui = new VGLobby.VGCharacterSetupWindowGui(profileEditor);

        windowGui.SelectCharacter += args =>
        {
            _preferencesManager.SelectCharacter(args);
            ReloadCharacterSetup();
            PreviewPanel?.UpdateCharacterSelector();
        };

        windowGui.DeleteCharacter += args =>
        {
            _preferencesManager.DeleteCharacter(args);
            PreviewPanel?.UpdateCharacterSelector();
        };

        profileEditor.Save += () =>
        {
            SaveProfileNew(profileEditor);
        };

        profileEditor.ProfileChanged += () =>
        {
            RefreshLobbyPreview();
            PreviewPanel?.UpdateCharacterSelector();
            windowGui.UpdatePreviewInstant();
        };

        _setupWindow = new VGLobby.VGCharacterSetupWindow(windowGui);

        _setupWindow.ClosingRequested += args =>
        {
            if (profileEditor.Profile != null && profileEditor.IsDirty)
            {
                args.Cancel = true;
                OpenSavePanelForNewWindow(profileEditor, windowGui);
            }
        };

        _setupWindow.OnClose += () =>
        {
            CleanupNewWindow();
        };

        _setupWindow.OpenCentered();

        profileEditor.SetProfile(
            (HumanoidCharacterProfile?)_preferencesManager.Preferences?.SelectedCharacter,
            _preferencesManager.Preferences?.SelectedCharacterIndex);

        windowGui.UpdatePreview();
        windowGui.UpdateNameField();
        windowGui.ReloadCharacterPickers();
        windowGui.UpdatePdaWallpaperSelection();
    }

    public VGCharacterSetupWindowGui? GetSetupWindow()
    {
        return _setupWindow?.Contents.GetChild(0) as VGCharacterSetupWindowGui;
    }

    private void CloseCharacterSetupNewWindow()
    {
        _setupWindow?.Close();
        CleanupNewWindow();
    }

    private void CleanupNewWindow()
    {
        if (_setupWindow != null)
        {
            _setupWindow.Dispose();
            _setupWindow = null;
        }
        _savePanelRequested = false;
    }

    public void RequestCharacterSwitch(int slot)
    {
        if (_setupWindow is not { IsOpen: true } ||
            _setupWindow.Contents.GetChild(0) is not VGLobby.VGCharacterSetupWindowGui gui)
        {
            _preferencesManager.SelectCharacter(slot);
            UpdateOpenCharacterSetupWindow();
            PreviewPanel?.UpdateCharacterSelector();
            return;
        }

        var editor = gui.GetProfileEditor();
        if (editor == null || !editor.IsDirty)
        {
            _preferencesManager.SelectCharacter(slot);
            UpdateOpenCharacterSetupWindow();
            PreviewPanel?.UpdateCharacterSelector();
            return;
        }

        _pendingSelectSlot = slot;
        OpenSavePanelForNewWindow(editor, gui);
    }

    private void OpenSavePanelForNewWindow(HumanoidProfileEditor editor, VGLobby.VGCharacterSetupWindowGui gui)
    {
        if (_savePanel is { IsOpen: true } || _savePanelRequested)
            return;

        _savePanelRequested = true;
        _savePanel = new CharacterSetupGuiSavePanel();

        _savePanel.OnClose += () =>
        {
            _savePanelRequested = false;
            _pendingSelectSlot = null;
        };

        _savePanel.SaveButton.OnPressed += _ =>
        {
            SaveProfileNew(editor);
            _savePanel.Close();

            if (_pendingSelectSlot.HasValue)
            {
                var slot = _pendingSelectSlot.Value;
                _pendingSelectSlot = null;
                _preferencesManager.SelectCharacter(slot);
                UpdateOpenCharacterSetupWindow();
                PreviewPanel?.UpdateCharacterSelector();
            }
            else
            {
                _setupWindow?.Close();
                CleanupNewWindow();
            }
        };

        _savePanel.NoSaveButton.OnPressed += _ =>
        {
            editor.ResetToDefault();
            _savePanel.Close();

            if (_pendingSelectSlot.HasValue)
            {
                var slot = _pendingSelectSlot.Value;
                _pendingSelectSlot = null;
                _preferencesManager.SelectCharacter(slot);
                UpdateOpenCharacterSetupWindow();
                PreviewPanel?.UpdateCharacterSelector();
            }
            else
            {
                _setupWindow?.Close();
                CleanupNewWindow();
            }
        };

        _savePanel.CancelButton.OnPressed += _ =>
        {
            _savePanel.Close();
            _pendingSelectSlot = null;
        };

        _savePanel.OpenCentered();
    }

    /// <summary>
    /// Обновляет открытое окно создания персонажа (если оно есть) при смене выбранного персонажа извне.
    /// </summary>
    public void UpdateOpenCharacterSetupWindow()
    {
        if (_setupWindow is { IsOpen: true } && _setupWindow.Contents.GetChild(0) is VGLobby.VGCharacterSetupWindowGui gui)
        {
            gui.UpdateProfileEditor();
            gui.UpdatePreview();
            gui.UpdateNameField();
            gui.ReloadCharacterPickers();
        }
    }
    // VG-Tweak End

    public void GiveDummyJobClothesLoadout(EntityUid dummy, JobPrototype? jobProto, HumanoidCharacterProfile profile)
    {
        var job = jobProto ?? GetPreferredJob(profile);
        GiveDummyJobClothes(dummy, profile, job);

        if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
        {
            var loadout = profile.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, profile.Species, EntityManager, _prototypeManager);
            GiveDummyLoadout(dummy, loadout);
        }
    }

    public JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? SharedGameTicker.FallbackOverflowJob);
    }

    public void GiveDummyLoadout(EntityUid uid, RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                _spawn.EquipStartingGear(uid, loadoutProto);
            }
        }

        _spawn.ApplyLoadoutExtras(uid, roleLoadout);
    }

    public void GiveDummyJobClothes(EntityUid dummy, HumanoidCharacterProfile profile, JobPrototype job)
    {
        if (!_inventory.TryGetSlots(dummy, out var slots))
            return;

        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    foreach (var slot in slots)
                    {
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntityManager.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                _inventory.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntityManager.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                _inventory.TryEquip(dummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (_inventory.TryUnequip(dummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntityManager.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntityManager.SpawnEntity(itemType, MapCoordinates.Nullspace);
                _inventory.TryEquip(dummy, item, slot.Name, true, true);
            }
        }
    }

    public EntityUid LoadProfileEntity(HumanoidCharacterProfile? humanoid, JobPrototype? job, bool jobClothes)
    {
        EntityUid dummyEnt;

        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes)
        {
            job ??= GetPreferredJob(humanoid);

            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job?.JobEntity;
        }

        if (previewEntity != null)
        {
            dummyEnt = EntityManager.SpawnEntity(previewEntity, MapCoordinates.Nullspace);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job?.ID)))
            {
                var loadout = humanoid?.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job?.ID), _playerManager.LocalSession, humanoid.Species, EntityManager, _prototypeManager);
                if (loadout != null)
                    _spawn.ApplyLoadoutExtras(dummyEnt, loadout);
            }

            return dummyEnt;
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index<SpeciesPrototype>(humanoid.Species).DollPrototype;
            dummyEnt = EntityManager.SpawnEntity(dummy, MapCoordinates.Nullspace);
        }
        else
        {
            dummyEnt = EntityManager.SpawnEntity(_prototypeManager.Index<SpeciesPrototype>(SharedHumanoidAppearanceSystem.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        _humanoid.LoadProfile(dummyEnt, humanoid);

        if (humanoid != null && jobClothes)
        {
            DebugTools.Assert(job != null);

            GiveDummyJobClothes(dummyEnt, humanoid, job);

            if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job.ID)))
            {
                var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, humanoid.Species, EntityManager, _prototypeManager);
                GiveDummyLoadout(dummyEnt, loadout);
            }
        }

        return dummyEnt;
    }
}