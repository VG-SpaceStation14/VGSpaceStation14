using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;
using Robust.Shared.Utility;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class SurgerySheetlet : Sheetlet<NanotrasenStylesheet>
{
    private static ResPath _textureRoot = new("/Textures/_VG/Interface/Targeting/Doll");

    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        return
        [
            // Head
            E<TextureButton>()
                .Class("TargetDollButtonHead")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "head_hover.png")),

            // Chest
            E<TextureButton>()
                .Class("TargetDollButtonChest")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "torso_hover.png")),

            // Groin
            E<TextureButton>()
                .Class("TargetDollButtonGroin")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "groin_hover.png")),

            // Left Arm
            E<TextureButton>()
                .Class("TargetDollButtonLeftArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "leftarm_hover.png")),

            // Left Hand
            E<TextureButton>()
                .Class("TargetDollButtonLeftHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "lefthand_hover.png")),

            // Right Arm
            E<TextureButton>()
                .Class("TargetDollButtonRightArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "rightarm_hover.png")),

            // Right Hand
            E<TextureButton>()
                .Class("TargetDollButtonRightHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "righthand_hover.png")),

            // Left Leg
            E<TextureButton>()
                .Class("TargetDollButtonLeftLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "leftleg_hover.png")),

            // Left Foot
            E<TextureButton>()
                .Class("TargetDollButtonLeftFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "leftfoot_hover.png")),

            // Right Leg
            E<TextureButton>()
                .Class("TargetDollButtonRightLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "rightleg_hover.png")),

            // Right Foot
            E<TextureButton>()
                .Class("TargetDollButtonRightFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "rightfoot_hover.png")),

            // Eyes
            E<TextureButton>()
                .Class("TargetDollButtonEyes")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "eyes_hover.png")),

            // Mouth
            E<TextureButton>()
                .Class("TargetDollButtonMouth")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, sheet.GetTexture(_textureRoot / "mouth_hover.png")),
        ];
    }
}