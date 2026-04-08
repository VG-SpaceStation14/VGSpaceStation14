using System.Linq;
using Content.Shared.ADT.MiningShop;
using Content.Shared.Mind;
using Content.Shared.ADT.Salvage.Systems;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static System.StringComparison;
using static Robust.Client.UserInterface.Controls.LineEdit;
using SharedMiningShopEntry = Content.Shared.ADT.MiningShop.MiningShopEntry;
using ClientMiningShopEntry = Content.Client.ADT.MiningShop.MiningShopEntry;

namespace Content.Client.ADT.MiningShop;

[UsedImplicitly]
public sealed class MiningShopBui : BoundUserInterface
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    private readonly MiningPointsSystem _miningPoints;
    private MiningShopWindow? _window;
    private List<SharedMiningShopSectionPrototype> _sections = new();
    private string? _selectedCategory;
    private Dictionary<string, Button> _categoryButtons = new();

    private static readonly Dictionary<EntityUid, string?> LastSelectedCategory = new();

    public MiningShopBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _miningPoints = EntMan.System<MiningPointsSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MiningShopWindow>();
        _window.OnClose += Close;
        _window.Title = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityName ?? "Магазин шахтёра";

        if (!EntMan.TryGetComponent(Owner, out MiningShopComponent? vendor))
            return;
        
        var sections = _prototype.EnumeratePrototypes<SharedMiningShopSectionPrototype>().ToList();
        sections.Sort((x, y) => string.Compare(x.ID, y.ID, StringComparison.Ordinal));
        _sections = sections;

        // Category buttons
        var allButton = new Button
        {
            Text = "ВСЕ",
            HorizontalExpand = true,
            ToggleMode = true,
            StyleClasses = { "ButtonSquare" },
            Pressed = true
        };
        allButton.OnToggled += args => OnAllCategorySelected(args.Pressed);
        _categoryButtons["ALL"] = allButton;

        // Распределяем кнопки по строкам
        var allButtons = new List<Button> { allButton };
        
        foreach (var section in sections)
        {
            var button = new Button
            {
                Text = section.Name.ToUpperInvariant(),
                HorizontalExpand = true,
                ToggleMode = true,
                StyleClasses = { "ButtonSquare" }
            };
            button.OnToggled += args => OnCategorySelected(section, args.Pressed);
            allButtons.Add(button);
            _categoryButtons[section.ID] = button;
        }

        // Равномерное распределение по двум строкам
        var halfCount = (allButtons.Count + 1) / 2;
        for (var i = 0; i < allButtons.Count; i++)
        {
            if (i < halfCount)
                _window.CategoryButtonsRow1.AddChild(allButtons[i]);
            else
                _window.CategoryButtonsRow2.AddChild(allButtons[i]);
        }

        // Добавляем спейсеры для выравнивания
        while (_window.CategoryButtonsRow1.ChildCount < halfCount)
            _window.CategoryButtonsRow1.AddChild(new Control { HorizontalExpand = true });
        while (_window.CategoryButtonsRow2.ChildCount < allButtons.Count - halfCount)
            _window.CategoryButtonsRow2.AddChild(new Control { HorizontalExpand = true });

        // Создаём секции
        foreach (var section in sections)
        {
            var uiSection = CreateSection(section);
            _window.Sections.AddChild(uiSection);
        }

        // Восстанавливаем последнюю категорию
        if (LastSelectedCategory.TryGetValue(Owner, out var lastCategory))
            SetSelectedCategory(lastCategory);

        _window.ClearCart.OnPressed += _ => OnClearCartPressed();
        _window.Express.OnPressed += _ => OnExpressDeliveryButtonPressed();
        _window.Search.OnTextChanged += OnSearchChanged;

        Refresh();
        _window.OpenCentered();
    }

    private void SetSelectedCategory(string? categoryId)
    {
        if (_window == null)
            return;

        _selectedCategory = categoryId;

        foreach (var kvp in _categoryButtons)
        {
            if (kvp.Key == "ALL")
                kvp.Value.Pressed = (categoryId == null);
            else
                kvp.Value.Pressed = (categoryId == kvp.Key);
        }

        LastSelectedCategory[Owner] = categoryId;
        UpdateSectionsVisibility();
    }

    private void OnAllCategorySelected(bool pressed)
    {
        if (pressed)
            SetSelectedCategory(null);
        else if (_sections.Count > 0)
            SetSelectedCategory(_sections[0].ID);
        else
            SetSelectedCategory(null);
    }

    private void OnCategorySelected(SharedMiningShopSectionPrototype section, bool pressed)
    {
        if (pressed)
            SetSelectedCategory(section.ID);
        else
            SetSelectedCategory(null);
    }

    private void UpdateSectionsVisibility()
    {
        if (_window == null)
            return;

        if (!string.IsNullOrWhiteSpace(_window.Search.Text))
        {
            ApplySearchFilter(_window.Search.Text);
            return;
        }

        for (var i = 0; i < _window.Sections.ChildCount; i++)
        {
            var section = _window.Sections.GetChild(i);
            if (section is MiningShopSection uiSection && i < _sections.Count)
            {
                var sectionData = _sections[i];
                uiSection.Visible = _selectedCategory == null || sectionData.ID == _selectedCategory;
            }
        }
    }

    private void ApplySearchFilter(string searchText)
    {
        if (_window == null)
            return;

        foreach (var sectionControl in _window.Sections.Children)
        {
            if (sectionControl is not MiningShopSection section)
                continue;

            var any = false;
            foreach (var entriesControl in section.Entries.Children)
            {
                if (entriesControl is not ClientMiningShopEntry entry)
                    continue;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    entry.Visible = true;
                }
                else
                {
                    var nameMatch = entry.ItemName.Text?.Contains(searchText, OrdinalIgnoreCase) ?? false;
                    var descMatch = false;
                    
                    if (entry.TooltipButton.ToolTip != null)
                        descMatch = entry.TooltipButton.ToolTip.Contains(searchText, OrdinalIgnoreCase);
                    
                    entry.Visible = nameMatch || descMatch;
                }

                if (entry.Visible)
                    any = true;
            }

            section.Visible = any;
        }
    }

    private MiningShopSection CreateSection(SharedMiningShopSectionPrototype section)
    {
        var uiSection = new MiningShopSection();
        uiSection.Label.SetMessage(GetSectionName(section));

        foreach (var entry in section.Entries)
        {
            var uiEntry = new ClientMiningShopEntry();

            if (_prototype.TryIndex(entry.Id, out var entity))
            {
                uiEntry.Texture.Textures = SpriteComponent.GetPrototypeTextures(entity, _resource)
                    .Select(o => o.Default)
                    .ToList();
                
                uiEntry.ItemName.Text = entry.Name?.Replace("\\n", " ") ?? entity.Name;
                
                var tooltipMsg = new FormattedMessage();
                tooltipMsg.AddText(entry.Name?.Replace("\\n", "\n") ?? entity.Name);
                if (!string.IsNullOrWhiteSpace(entity.Description))
                {
                    tooltipMsg.PushNewline();
                    tooltipMsg.AddText(entity.Description);
                }
                
                var tooltip = new Tooltip();
                tooltip.SetMessage(tooltipMsg);
                tooltip.MaxWidth = 250f;
                
                uiEntry.TooltipButton.ToolTip = entity.Description ?? "";
                uiEntry.TooltipButton.TooltipDelay = 0;
                uiEntry.TooltipButton.TooltipSupplier = _ => tooltip;

                uiEntry.SetBackgroundColor(Color.FromHex("#162031"), Color.FromHex("#4972A1"));
                uiEntry.BuyButton.OnPressed += _ => OnAddToCartPressed(entry);
            }

            uiSection.Entries.AddChild(uiEntry);
        }

        return uiSection;
    }

    private void OnAddToCartPressed(SharedMiningShopEntry entry)
    {
        SendMessage(new MiningShopBuiMsg(entry));
    }

    private void OnClearCartPressed()
    {
        SendMessage(new MiningShopClearCartBuiMsg());
    }

    private void OnExpressDeliveryButtonPressed()
    {
        SendMessage(new MiningShopExpressDeliveryBuiMsg());
        Refresh();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        if (_window == null)
            return;

        if (string.IsNullOrWhiteSpace(args.Text))
            UpdateSectionsVisibility();
        else
            ApplySearchFilter(args.Text);
    }

    public void Refresh()
    {
        if (_window == null || _player.LocalEntity == null)
            return;

        if (!EntMan.TryGetComponent(Owner, out MiningShopComponent? vendor))
            return;

        var user = _player.LocalEntity.Value;

        List<SharedMiningShopEntry> userOrders = new();
        if (EntMan.System<SharedMiningShopSystem>().TryGetUserOrders(Owner, user, out var orders))
            userOrders = orders;

        var names = userOrders.Select(entry =>
        {
            var name = _prototype.TryIndex(entry.Id, out var entity) ? entity.Name : entry.Name;
            return name ?? "?";
        }).ToList();
        var ordersString = string.Join(", ", names);

        var userpoints = _miningPoints.TryFindIdCard(user)?.Comp?.Points ?? 0;

        bool hasOrders = userOrders.Count > 0;
        uint totalCost = 0;
        foreach (var entry in userOrders)
            totalCost += entry.Price ?? 0;
        
        _window.TotalCostLabel.Text = $"Сумма: {totalCost} P";
        _window.TotalCostLabel.Visible = hasOrders;
        _window.ClearCart.Visible = hasOrders;
        _window.Express.Visible = hasOrders;

        _window.YourPurchases.SetMessage(userOrders.Count > 0 ? $"Заказы: {ordersString}" : "");
        _window.PointsLabel.Text = $"Очки: {userpoints}";

        for (var sectionIndex = 0; sectionIndex < _sections.Count; sectionIndex++)
        {
            var section = _sections[sectionIndex];
            var uiSection = (MiningShopSection)_window.Sections.GetChild(sectionIndex);
            uiSection.Label.SetMessage(GetSectionName(section));

            for (var entryIndex = 0; entryIndex < section.Entries.Count; entryIndex++)
            {
                var entry = section.Entries[entryIndex];
                var uiEntry = (ClientMiningShopEntry)uiSection.Entries.GetChild(entryIndex);
                var price = entry.Price ?? 0;
                var disabled = userpoints < price;

                uiEntry.Price.Text = $"{price} P";
                uiEntry.BuyButton.Disabled = disabled;
                uiEntry.SetDisabled(disabled);
            }
        }

        if (!string.IsNullOrWhiteSpace(_window.Search.Text))
            ApplySearchFilter(_window.Search.Text);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is MiningShopRefreshBuiMsg)
            Refresh();
    }

    private FormattedMessage GetSectionName(SharedMiningShopSectionPrototype section)
    {
        var name = new FormattedMessage();
        name.PushTag(new MarkupNode("bold", new MarkupParameter(section.Name.ToUpperInvariant()), null));
        name.AddText(section.Name.ToUpperInvariant());
        name.Pop();
        return name;
    }
}