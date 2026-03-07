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

    public MiningShopBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _miningPoints = EntMan.System<MiningPointsSystem>();
    }

    protected override void Open()
    {
        _window = new MiningShopWindow();
        _window.OnClose += Close;
        _window.Title = EntMan.GetComponentOrNull<MetaDataComponent>(Owner)?.EntityName ?? "MiningShop";

        if (!EntMan.TryGetComponent(Owner, out MiningShopComponent? vendor))
            return;
        
        var sections = _prototype.EnumeratePrototypes<SharedMiningShopSectionPrototype>().ToList();
        sections.Sort((x, y) => x.Name[0].CompareTo(x.Name[0]));
        _sections = sections;

        // Add "All" category button at the beginning
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

        // Create category buttons in two rows with uniform width
        var halfCount = (sections.Count + 2) / 2; // +1 for All button, then /2
        var firstRowButtons = new List<Button>();
        var secondRowButtons = new List<Button>();

        firstRowButtons.Add(allButton);

        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var button = new Button
            {
                Text = section.Name.ToUpperInvariant(),
                HorizontalExpand = true,
                ToggleMode = true,
                StyleClasses = { "ButtonSquare" }
            };
            button.OnToggled += args => OnCategorySelected(section, args.Pressed);
            
            if (firstRowButtons.Count < halfCount)
                firstRowButtons.Add(button);
            else
                secondRowButtons.Add(button);
            
            _categoryButtons[section.ID] = button;
        }

        foreach (var button in firstRowButtons)
            _window.CategoryButtonsRow1.AddChild(button);
            
        foreach (var button in secondRowButtons)
            _window.CategoryButtonsRow2.AddChild(button);

        // Add spacers if needed to maintain alignment
        if (firstRowButtons.Count < halfCount)
        {
            var spacerCount = halfCount - firstRowButtons.Count;
            for (var i = 0; i < spacerCount; i++)
                _window.CategoryButtonsRow1.AddChild(new Control { HorizontalExpand = true });
        }
        
        if (secondRowButtons.Count < sections.Count + 1 - halfCount)
        {
            var spacerCount = (sections.Count + 1 - halfCount) - secondRowButtons.Count;
            for (var i = 0; i < spacerCount; i++)
                _window.CategoryButtonsRow2.AddChild(new Control { HorizontalExpand = true });
        }

        foreach (var section in sections)
        {
            var uiSection = CreateSection(section);
            _window.Sections.AddChild(uiSection);
        }

        _window.ClearCart.OnPressed += _ => OnClearCartPressed();
        _window.Express.OnPressed += _ => OnExpressDeliveryButtonPressed();
        _window.Search.OnTextChanged += OnSearchChanged;

        Refresh();

        _window.OpenCentered();
    }

    private void OnAllCategorySelected(bool pressed)
    {
        if (pressed)
        {
            _selectedCategory = null;
            foreach (var btn in _categoryButtons.Values)
            {
                if (btn != _categoryButtons["ALL"])
                    btn.Pressed = false;
            }
        }
        else
        {
            if (_sections.Count > 0)
            {
                var firstSection = _sections[0];
                _selectedCategory = firstSection.ID;
                _categoryButtons[firstSection.ID].Pressed = true;
            }
        }
        
        UpdateSectionsVisibility();
    }

    private void OnCategorySelected(SharedMiningShopSectionPrototype section, bool pressed)
    {
        if (pressed)
        {
            _selectedCategory = section.ID;
            foreach (var btn in _categoryButtons.Values)
            {
                if (btn.Text != section.Name.ToUpperInvariant() && btn != _categoryButtons["ALL"])
                    btn.Pressed = false;
            }
            _categoryButtons["ALL"].Pressed = false;
        }
        else
        {
            _selectedCategory = null;
            _categoryButtons["ALL"].Pressed = true;
        }
        
        UpdateSectionsVisibility();
    }

    private void UpdateSectionsVisibility()
    {
        if (_window == null)
            return;

        if (_window.Search != null && !string.IsNullOrWhiteSpace(_window.Search.Text))
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
                    entry.Visible = true;
                else
                {
                    var nameMatch = entry.ItemName.Text?.Contains(searchText, OrdinalIgnoreCase) ?? false;
                    var descMatch = entry.Description.Text?.Contains(searchText, OrdinalIgnoreCase) ?? false;
                    entry.Visible = nameMatch || descMatch;
                }

                if (entry.Visible)
                    any = true;
            }

            section.Visible = any;
        }
    }

    private void ResetSearchFilter()
    {
        if (_window == null)
            return;
        foreach (var sectionControl in _window.Sections.Children)
        {
            if (sectionControl is not MiningShopSection section)
                continue;
            foreach (var entriesControl in section.Entries.Children)
            {
                if (entriesControl is ClientMiningShopEntry entry)
                    entry.Visible = true;
            }
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
                
                uiEntry.ItemName.Text = entry.Name?.Replace("\\n", "\n") ?? entity.Name;
                
                var description = new FormattedMessage();
                if (!string.IsNullOrWhiteSpace(entity.Description))
                    description.AddText(entity.Description);
                uiEntry.Description.SetMessage(description);

                uiEntry.SetBackgroundColor(Color.FromHex("#162031"), Color.FromHex("#4972A1"));

                uiEntry.BuyButton.OnPressed += _ => OnAddToCartPressed(entry);
            }

            uiSection.Entries.AddChild(uiEntry);
        }

        return uiSection;
    }

    private void OnAddToCartPressed(SharedMiningShopEntry entry)
    {
        var msg = new MiningShopBuiMsg(entry);
        SendMessage(msg);
    }

    private void OnClearCartPressed()
    {
        var msg = new MiningShopClearCartBuiMsg();
        SendMessage(msg);
    }

    private void OnExpressDeliveryButtonPressed()
    {
        var msg = new MiningShopExpressDeliveryBuiMsg();
        SendMessage(msg);
        Refresh();
    }

    private void OnSearchChanged(LineEditEventArgs args)
    {
        if (_window == null)
            return;

        if (string.IsNullOrWhiteSpace(args.Text))
        {
            ResetSearchFilter();
            UpdateSectionsVisibility();
        }
        else
        {
            ApplySearchFilter(args.Text);
        }
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

        // Calculate total cost and show/hide elements
        bool hasOrders = userOrders.Count > 0;
        uint totalCost = 0;
        foreach (var entry in userOrders)
        {
            totalCost += entry.Price ?? 0;
        }
        
        _window.TotalCostLabel.Text = $"Сумма: {totalCost} P";
        _window.TotalCostLabel.Visible = hasOrders;
        _window.ClearCart.Visible = hasOrders;

        _window.YourPurchases.Text = $"Заказы: {ordersString}";
        _window.PointsLabel.Text = $"Осталось очков: {userpoints}";

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

        if (_window.Search != null && !string.IsNullOrWhiteSpace(_window.Search.Text))
        {
            ApplySearchFilter(_window.Search.Text);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _window?.Dispose();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        switch (message)
        {
            case MiningShopRefreshBuiMsg:
                Refresh();
                break;
        }
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