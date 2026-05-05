using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Inventory;
using Content.Shared.Access.Components;
using Content.Shared.Station.Components;
using Content.Shared.PDA;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Humanoid;
using Robust.Shared.Enums;

namespace Content.Client._VG.Arrival;

public sealed class VGArrivalOverlay : Control
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private readonly Label _label;

    private float _bgAlpha = 1f;
    private float _elementsAlpha = 1f;

    private readonly float _bgFadeInSpeed = 0.8f;
    private readonly float _finalFadeOutSpeed = 1.2f;

    private string? _fullText;
    private int _charIndex;
    private float _typeTimer;
    private readonly float _typeDelay = 0.12f;

    private float _holdTimer = 2.5f;

    private float _waitForIdTimer = 3.0f;
    private bool _textReady = false;

    private readonly EntityUid _playerEntity;
    private bool _disposing;

    private enum State { BackgroundFadeOut, Typing, Holding, FinalFadeOut, Disposed }
    private State _currentState = State.BackgroundFadeOut;

    public VGArrivalOverlay(EntityUid entity, string fallbackName)
    {
        IoCManager.InjectDependencies(this);

        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);
        MouseFilter = MouseFilterMode.Ignore;

        _playerEntity = entity;

        _label = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.White,
            Text = ""
        };

        AddChild(_label);

        TryBuildFullText(entity, fallbackName);
    }

    private void TryBuildFullText(EntityUid entity, string fallbackName, bool force = false)
    {
        var name = fallbackName;
        if (_entMan.TryGetComponent(entity, out MetaDataComponent? meta))
            name = meta.EntityName;

        string? jobTitle = null;
        if (_entMan.TryGetComponent<InventoryComponent>(entity, out var inventory))
        {
            var inventorySystem = _entMan.System<InventorySystem>();
            if (inventorySystem.TryGetSlotEntity(entity, "id", out var slotEntity, inventory))
            {
                if (_entMan.TryGetComponent<IdCardComponent>(slotEntity.Value, out var directId))
                {
                    jobTitle = directId.LocalizedJobTitle;
                }
                else if (_entMan.TryGetComponent<PdaComponent>(slotEntity.Value, out var pda))
                {
                    var itemSlots = _entMan.System<ItemSlotsSystem>();
                    var idInsidePda = itemSlots.GetItemOrNull(slotEntity.Value, PdaComponent.PdaIdSlotId);
                    if (idInsidePda != null && _entMan.TryGetComponent<IdCardComponent>(idInsidePda.Value, out var nestedId))
                    {
                        jobTitle = nestedId.LocalizedJobTitle;
                    }
                }
            }
        }

        var arrivedVerb = "ПРИБЫЛ";
        if (_entMan.TryGetComponent<HumanoidAppearanceComponent>(entity, out var humanoid))
        {
            arrivedVerb = humanoid.Gender switch
            {
                Gender.Male => "ПРИБЫЛ",
                Gender.Female => "ПРИБЫЛА",
                Gender.Neuter => "ПРИБЫЛО",
                Gender.Epicene => "ПРИБЫЛИ",
                _ => "ПРИБЫЛ"
            };
        }

        var stationName = "Неизвестная станция";
        var transform = _entMan.GetComponent<TransformComponent>(entity);
        var parent = transform.ParentUid;
        while (parent.IsValid())
        {
            if (_entMan.TryGetComponent<StationMemberComponent>(parent, out _))
            {
                if (_entMan.TryGetComponent<MetaDataComponent>(parent, out var stationMeta))
                {
                    stationName = stationMeta.EntityName;
                }
                break;
            }

            if (!_entMan.TryGetComponent<TransformComponent>(parent, out var parentTransform))
                break;
            parent = parentTransform.ParentUid;
        }

        if (jobTitle == null && !force)
        {
            _fullText = null;
            return;
        }

        var finalJob = jobTitle ?? "Неизвестно";
        _fullText = $"{name}, {finalJob} - {arrivedVerb}\nНА СТАНЦИЮ {stationName}";
        _textReady = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (_disposing)
            return;

        base.Draw(handle);
        var rect = SizeBox;

        if (_bgAlpha > 0)
            handle.DrawRect(rect, Color.Black.WithAlpha(_bgAlpha));

        var barHeight = rect.Height * 0.12f;
        var barColor = Color.Black.WithAlpha(_elementsAlpha);
        handle.DrawRect(new UIBox2(0, 0, rect.Width, barHeight), barColor);
        handle.DrawRect(new UIBox2(0, rect.Height - barHeight, rect.Width, rect.Height), barColor);

        _label.FontColorOverride = Color.White.WithAlpha(_elementsAlpha);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        if (_disposing)
            return;

        base.FrameUpdate(args);
        var dt = (float)args.DeltaSeconds;

        switch (_currentState)
        {
            case State.BackgroundFadeOut:
                _bgAlpha -= dt * _bgFadeInSpeed;

                if (!_textReady)
                {
                    _waitForIdTimer -= dt;
                    if (_waitForIdTimer <= 0f)
                        TryBuildFullText(_playerEntity, "???", force: true);
                    else
                        TryBuildFullText(_playerEntity, "???");
                }

                if (_bgAlpha <= 0f && _textReady)
                {
                    _bgAlpha = 0f;
                    _currentState = State.Typing;
                }
                break;

            case State.Typing:
                _typeTimer += dt;
                if (_typeTimer >= _typeDelay && _fullText != null && _charIndex < _fullText.Length)
                {
                    _typeTimer = 0f;
                    _charIndex++;
                    _label.Text = _fullText.Substring(0, _charIndex);
                }

                if (_fullText != null && _charIndex >= _fullText.Length)
                    _currentState = State.Holding;
                break;

            case State.Holding:
                _holdTimer -= dt;
                if (_holdTimer <= 0)
                    _currentState = State.FinalFadeOut;
                break;

            case State.FinalFadeOut:
                _elementsAlpha -= dt * _finalFadeOutSpeed;
                if (_elementsAlpha <= 0f)
                {
                    _elementsAlpha = 0f;
                    _currentState = State.Disposed;
                }
                break;

            case State.Disposed:
                if (!_disposing)
                {
                    _disposing = true;
                    _uiManager.DeferAction(() => Dispose());
                }
                break;
        }
    }
}