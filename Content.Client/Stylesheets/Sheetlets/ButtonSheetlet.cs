using System.Numerics;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class ButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig buttonCfg = sheet;
        IIconConfig iconCfg = sheet;

        var crossTex = sheet.GetTextureOr(iconCfg.CrossIconPath, NanotrasenStylesheet.TextureRoot);
        var refreshTex = sheet.GetTextureOr(iconCfg.RefreshIconPath, NanotrasenStylesheet.TextureRoot);

        var rules = new List<StyleRule>
        {
            CButton()
                .Box(StyleBoxHelpers.RoundedStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Box(StyleBoxHelpers.RoundedOpenLeftStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Box(StyleBoxHelpers.RoundedOpenRightStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Box(StyleBoxHelpers.RoundedSquareStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Box(StyleBoxHelpers.RoundedSquareStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .Box(StyleBoxHelpers.RoundedSmallStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSmall)
                .ParentOf(E<Label>())
                .Font(sheet.BaseFont.GetFont(8)),
            CButton().Class(StyleClass.ButtonBig).ParentOf(E<Label>()).Font(sheet.BaseFont.GetFont(16)),

            E<TextureButton>()
                .Class(StyleClass.CrossButtonRed)
                .Prop(TextureButton.StylePropertyTexture, crossTex),

            E<TextureButton>()
                .Class(StyleClass.RefreshButton)
                .Prop(TextureButton.StylePropertyTexture, refreshTex),

            E<Label>()
                .Class(Button.StyleClassButton)
                .AlignMode(Label.AlignMode.Center),

            CButton().PseudoDisabled().ParentOf(E<Label>()).FontColor(Color.FromHex("#E5E5E581")),
            CButton().PseudoDisabled().ParentOf(E()).ParentOf(E<Label>()).FontColor(Color.FromHex("#E5E5E581")),
        };
        
        MakeButtonRules<TextureButton>(rules, Palettes.AlphaModulate, null);
        MakeButtonRules<TextureButton>(rules, sheet.NegativePalette, StyleClass.CrossButtonRed);

        MakeButtonRules(rules, buttonCfg.ButtonPalette, null);
        MakeButtonRules(rules, buttonCfg.PositiveButtonPalette, StyleClass.Positive);
        MakeButtonRules(rules, buttonCfg.NegativeButtonPalette, StyleClass.Negative);

        return rules.ToArray();
    }

    public static void MakeButtonRules<TC>(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
        where TC : Control
    {
        rules.AddRange([
            E<TC>().MaybeClass(styleclass).PseudoNormal().Modulate(palette.Element),
            E<TC>().MaybeClass(styleclass).PseudoHovered().Modulate(palette.HoveredElement),
            E<TC>().MaybeClass(styleclass).PseudoPressed().Modulate(palette.PressedElement),
            E<TC>().MaybeClass(styleclass).PseudoDisabled().Modulate(palette.DisabledElement),
        ]);
    }

    public static void MakeButtonRules(
        List<StyleRule> rules,
        ColorPalette palette,
        string? styleclass)
    {
        rules.AddRange([
            E().MaybeClass(styleclass).PseudoNormal().Prop(Control.StylePropertyModulateSelf, palette.Element),
            E().MaybeClass(styleclass).PseudoHovered().Prop(Control.StylePropertyModulateSelf, palette.HoveredElement),
            E().MaybeClass(styleclass).PseudoPressed().Prop(Control.StylePropertyModulateSelf, palette.PressedElement),
            E()
                .MaybeClass(styleclass)
                .PseudoDisabled()
                .Prop(Control.StylePropertyModulateSelf, palette.DisabledElement),
        ]);
    }

    private static MutableSelectorElement CButton()
    {
        return E<ContainerButton>().Class(ContainerButton.StyleClassButton);
    }
}

public static class StyleBoxHelpers
{
    private const int CornerRadius = 3;
    
    // Новые закругленные методы
    public static StyleBoxTexture RoundedStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var baseBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot),
        };
        baseBox.SetPatchMargin(StyleBox.Margin.All, 5);
        baseBox.SetPadding(StyleBox.Margin.All, 1);
        baseBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);
        baseBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 14);
        return baseBox;
    }

    public static StyleBoxTexture RoundedOpenLeftStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openLeftBox = new StyleBoxTexture(RoundedStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(5, 0), new Vector2(14, 24))),
        };
        openLeftBox.SetPatchMargin(StyleBox.Margin.Left, 0);
        openLeftBox.SetContentMarginOverride(StyleBox.Margin.Left, 8);
        return openLeftBox;
    }

    public static StyleBoxTexture RoundedOpenRightStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openRightBox = new StyleBoxTexture(RoundedStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        openRightBox.SetPatchMargin(StyleBox.Margin.Right, 0);
        openRightBox.SetContentMarginOverride(StyleBox.Margin.Right, 8);
        openRightBox.SetPadding(StyleBox.Margin.Right, 1);
        return openRightBox;
    }

    public static StyleBoxTexture RoundedSquareStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var openBothBox = new StyleBoxTexture(RoundedStyleBox(sheet))
        {
            Texture = new AtlasTexture(sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot),
                UIBox2.FromDimensions(new Vector2(5, 0), new Vector2(3, 24))),
        };
        openBothBox.SetPatchMargin(StyleBox.Margin.Horizontal, 0);
        openBothBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 8);
        openBothBox.SetPadding(StyleBox.Margin.Horizontal, 1);
        return openBothBox;
    }

    public static StyleBoxTexture RoundedSmallStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
    {
        var smallBox = new StyleBoxTexture
        {
            Texture = sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/button_small.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot),
        };
        return smallBox;
    }

    // Совместимые методы для старого кода (например, PdaSheetlet)
    public static StyleBoxTexture BaseStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
        => RoundedStyleBox(sheet);
    public static StyleBoxTexture SquareStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
        => RoundedSquareStyleBox(sheet);
    public static StyleBoxTexture OpenLeftStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
        => RoundedOpenLeftStyleBox(sheet);
    public static StyleBoxTexture OpenRightStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
        => RoundedOpenRightStyleBox(sheet);
    public static StyleBoxTexture SmallStyleBox<T>(T sheet) where T : PalettedStylesheet, IButtonConfig
        => RoundedSmallStyleBox(sheet);
}