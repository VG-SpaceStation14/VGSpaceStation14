using Content.Client.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Content.Client.Stylesheets;

namespace Content.Client.ADT.Language.UI;

[CommonSheetlet]
public sealed class LanguagesSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        var bgDark = Color.FromHex("#1a1a22");
        var bgMedium = Color.FromHex("#22222a");
        var bgLight = Color.FromHex("#2a2a35");
        var textPrimary = Color.FromHex("#e0e0e0");
        var textSecondary = Color.FromHex("#a0a0a0");
        var accentBlue = Color.FromHex("#60a5fa");

        var headerPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgLight,
            BorderColor = bgMedium,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        var entryPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgLight,
            BorderColor = bgMedium,
            BorderThickness = new Thickness(1)
        };

        var progressBarBgBox = new StyleBoxFlat
        {
            BackgroundColor = bgDark,
            BorderColor = bgMedium,
            BorderThickness = new Thickness(1)
        };
        var progressBarFillBox = new StyleBoxFlat { BackgroundColor = accentBlue };

        var searchBarBox = new StyleBoxFlat { BackgroundColor = bgMedium };
        var searchInputBox = new StyleBoxFlat { BackgroundColor = bgDark };
        searchInputBox.SetContentMarginOverride(StyleBox.Margin.All, 8);

        var footerPanelBox = new StyleBoxFlat
        {
            BackgroundColor = bgMedium,
            BorderColor = bgMedium,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        var expandButtonBox = new StyleBoxFlat { BackgroundColor = Color.Transparent };
        expandButtonBox.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var descriptionPanelBox = new StyleBoxFlat { BackgroundColor = bgDark };
        descriptionPanelBox.SetContentMarginOverride(StyleBox.Margin.All, 4);

        var rules = new List<StyleRule>
        {
            // Header
            E<PanelContainer>().Class("LanguagesHeaderPanel").Panel(headerPanelBox),
            E<Label>().Class("LanguagesTitleLabel").Font(sheet.BaseFont.GetFont(14)).FontColor(textPrimary),
            E<Label>().Class("LanguagesStatLabel").Font(sheet.BaseFont.GetFont(12)).FontColor(accentBlue),

            // Progress bar
            E<PanelContainer>().Class("LanguagesProgressBarBg").Panel(progressBarBgBox),
            E<PanelContainer>().Class("LanguagesProgressBarFill").Panel(progressBarFillBox),

            // Search
            E<PanelContainer>().Class("LanguagesSearchBar").Panel(searchBarBox),
            E<LineEdit>().Class("LanguagesSearchInput").Panel(searchInputBox),

            // Footer
            E<PanelContainer>().Class("LanguagesFooterPanel").Panel(footerPanelBox),
            E<Label>().Class("LanguagesFooterText").Font(sheet.BaseFont.GetFont(10)).FontColor(textSecondary),

            // Language entry
            E<PanelContainer>().Class("LanguagesEntryPanel").Panel(entryPanelBox),
            E<Label>().Class("LanguagesEntryNameLabel").Font(sheet.BaseFont.GetFont(11)).FontColor(textPrimary),
            E<Button>().Class("LanguagesEntryButton"),
            E<Button>().Class("LanguagesEntryExpandButton").Prop(ContainerButton.StylePropertyStyleBox, expandButtonBox),
            E<RichTextLabel>().Class("LanguagesEntryDescriptionLabel").Font(sheet.BaseFont.GetFont(10)).FontColor(textSecondary),
            E<PanelContainer>().Class("LanguagesEntryDescriptionPanel").Panel(descriptionPanelBox)
        };

        return rules.ToArray();
    }
}