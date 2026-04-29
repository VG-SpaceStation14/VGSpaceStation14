using Content.Client._VG.UserInterface.Systems.PartStatus.Widgets;
using Content.Client.Gameplay;
using Content.Shared._VG.Targeting;
using Content.Shared._VG.Surgery;
using Content.Client._VG.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;
using Robust.Client.Graphics;

namespace Content.Client._VG.UserInterface.Systems.PartStatus;

public sealed class PartStatusUIController : UIController,
    IOnStateEntered<GameplayState>,
    IOnSystemChanged<TargetingSystem>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private SpriteSystem _spriteSystem = default!;
    private TargetingComponent? _targetingComponent;
    private EntityUid? _target;

    private PartStatusControl? PartStatusControl =>
        UIManager.GetActiveUIWidgetOrNull<PartStatusControl>();

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.PartStatusStartup += AddPartStatusControl;
        system.PartStatusShutdown += RemovePartStatusControl;
        system.PartStatusUpdate += UpdatePartStatusControl;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.PartStatusStartup -= AddPartStatusControl;
        system.PartStatusShutdown -= RemovePartStatusControl;
        system.PartStatusUpdate -= UpdatePartStatusControl;
    }

    public void OnStateEntered(GameplayState state)
    {
        UpdateUI();
    }

    public void AddPartStatusControl(TargetingComponent component)
    {
        _targetingComponent = component;
        _target = component.Owner;

        UpdateUI();
    }

    public void RemovePartStatusControl()
    {
        if (PartStatusControl != null)
            PartStatusControl.SetVisible(false);

        _targetingComponent = null;
        _target = null;
    }

    public void UpdatePartStatusControl(TargetingComponent component)
    {
        _targetingComponent = component;
        _target = component.Owner;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (PartStatusControl == null || _targetingComponent == null)
            return;

        string dollPath = "/Textures/_VG/Interface/Targeting/Status/Human";

        if (_target is not null &&
            _entManager.TryGetComponent<SurgeryTargetComponent>(_target.Value, out var surgery))
        {
            if (!string.IsNullOrEmpty(surgery.DollPath))
                dollPath = surgery.DollPath;
        }

        PartStatusControl.SetVisible(true);

        PartStatusControl.SetTextures(
            _targetingComponent.BodyStatus,
            dollPath
        );
    }

    public Texture GetTexture(SpriteSpecifier specifier)
    {
        if (_spriteSystem == null)
            _spriteSystem = _entManager.System<SpriteSystem>();

        return _spriteSystem.Frame0(specifier);
    }
}