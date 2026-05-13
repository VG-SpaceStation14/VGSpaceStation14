using System.Numerics;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class MenuButtonSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet, IButtonConfig, IIconConfig
{
    private static MutableSelectorElement CButton()
    {
        return E<MenuButton>();
    }

    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig cfg = sheet;

        var buttonTex = sheet.GetTextureOr(new ResPath("/Textures/Interface/Nano/rounded_button.svg.96dpi.png"), NanotrasenStylesheet.TextureRoot);
        var topButtonBase = new StyleBoxTexture
        {
            Texture = buttonTex,
        };
        topButtonBase.SetPatchMargin(StyleBox.Margin.All, 5);
        topButtonBase.SetPadding(StyleBox.Margin.All, 0);
        topButtonBase.SetContentMarginOverride(StyleBox.Margin.All, 0);

        var topButtonOpenRight = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(0, 0), new Vector2(14, 24))),
        };
        topButtonOpenRight.SetPatchMargin(StyleBox.Margin.Right, 0);

        var topButtonOpenLeft = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(5, 0), new Vector2(14, 24))),
        };
        topButtonOpenLeft.SetPatchMargin(StyleBox.Margin.Left, 0);

        var topButtonSquare = new StyleBoxTexture(topButtonBase)
        {
            Texture = new AtlasTexture(buttonTex, UIBox2.FromDimensions(new Vector2(5, 0), new Vector2(3, 24))),
        };
        topButtonSquare.SetPatchMargin(StyleBox.Margin.Horizontal, 0);

        var rules = new List<StyleRule>
        {
            CButton().Class(StyleClass.ButtonSquare).Box(topButtonSquare),
            CButton().Class(StyleClass.ButtonOpenLeft).Box(topButtonOpenLeft),
            CButton().Class(StyleClass.ButtonOpenRight).Box(topButtonOpenRight),
            CButton().Box(StyleBoxHelpers.RoundedStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.RoundedOpenLeftStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.RoundedOpenRightStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.RoundedSquareStyleBox(sheet)),
            CButton()
                .Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, StyleBoxHelpers.RoundedSquareStyleBox(sheet)),
            E<Label>()
                .Class(MenuButton.StyleClassLabelTopButton)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(14, FontKind.Bold)),
        };

        ButtonSheetlet<T>.MakeButtonRules<MenuButton>(rules, cfg.ButtonPalette, null);

        return rules.ToArray();
    }
}