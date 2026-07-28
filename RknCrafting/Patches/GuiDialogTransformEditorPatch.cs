using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace RKN.Crafting.Patches;

//[HarmonyPatch(typeof(GuiDialogTransformEditor), "ComposeDialog")]
public class GuiDialogTransformEditorPatch
{
    private static MethodInfo OnTitleBarCloseMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnTitleBarClose");
    private static MethodInfo OnTabClickedMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnTabClicked");
    private static MethodInfo OnOriginXMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnOriginX");
    private static MethodInfo OnOriginYMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnOriginY");
    private static MethodInfo OnOriginZMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnOriginZ");
    private static MethodInfo OnTranslateXMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnTranslateX");
    private static MethodInfo OnTranslateYMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnTranslateY");
    private static MethodInfo OnTranslateZMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnTranslateZ");
    private static MethodInfo OnRotateXMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnRotateX");
    private static MethodInfo OnRotateYMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnRotateY");
    private static MethodInfo OnRotateZMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnRotateZ");
    private static MethodInfo OnScaleMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnScale");
    private static MethodInfo onFlipXAxisMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "onFlipXAxis");
    private static MethodInfo onFlipByTypeJsonMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "onFlipByTypeJson");
    private static MethodInfo OnNextSlotMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnNextSlot");
    private static MethodInfo OnApplyJsonMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnApplyJson");
    private static MethodInfo OnResetJsonMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnResetJson");
    private static MethodInfo OnCopyJsonMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnCopyJson");
    private static MethodInfo OnCopyInnerJsonMethod = AccessTools.DeclaredMethod(typeof(GuiDialogTransformEditor), "OnCopyInnerJson");
  
  
    static bool Prefix(GuiDialogTransformEditor __instance, ICoreClientAPI ___capi, ModelTransform ___currentTransform, bool ___byTypeJson, int ___target)
    {
        Action OnTitleBarClose = (Action) Delegate.CreateDelegate(typeof(Action), __instance, OnTitleBarCloseMethod);
        Action<int, GuiTab> OnTabClicked = (Action<int, GuiTab>) Delegate.CreateDelegate(typeof(Action<int, GuiTab>), __instance, OnTabClickedMethod);
        Action<string> OnTranslateX = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnTranslateXMethod);
        Action<string> OnTranslateY = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnTranslateYMethod);
        Action<string> OnTranslateZ = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnTranslateZMethod);
        Action<string> OnOriginX = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnOriginXMethod);
        Action<string> OnOriginY = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnOriginYMethod);
        Action<string> OnOriginZ = (Action<string>) Delegate.CreateDelegate(typeof(Action<string>), __instance, OnOriginZMethod);
        ActionConsumable<int> OnRotateX = (ActionConsumable<int>) Delegate.CreateDelegate(typeof(ActionConsumable<int>), __instance, OnRotateXMethod);
        ActionConsumable<int> OnRotateY = (ActionConsumable<int>) Delegate.CreateDelegate(typeof(ActionConsumable<int>), __instance, OnRotateYMethod);
        ActionConsumable<int> OnRotateZ = (ActionConsumable<int>) Delegate.CreateDelegate(typeof(ActionConsumable<int>), __instance, OnRotateZMethod);
        ActionConsumable<int> OnScale = (ActionConsumable<int>) Delegate.CreateDelegate(typeof(ActionConsumable<int>), __instance, OnScaleMethod);
        Action<bool> onFlipXAxis = (Action<bool>) Delegate.CreateDelegate(typeof(Action<bool>), __instance, onFlipXAxisMethod);
        Action<bool> onFlipByTypeJson = (Action<bool>) Delegate.CreateDelegate(typeof(Action<bool>), __instance, onFlipByTypeJsonMethod);
        ActionConsumable OnNextSlot = (ActionConsumable) Delegate.CreateDelegate(typeof(ActionConsumable), __instance, OnNextSlotMethod);
        ActionConsumable OnApplyJson = (ActionConsumable) Delegate.CreateDelegate(typeof(ActionConsumable), __instance, OnApplyJsonMethod);
        ActionConsumable OnResetJson = (ActionConsumable) Delegate.CreateDelegate(typeof(ActionConsumable), __instance, OnResetJsonMethod);
        ActionConsumable OnCopyJson = (ActionConsumable) Delegate.CreateDelegate(typeof(ActionConsumable), __instance, OnCopyJsonMethod);
        ActionConsumable OnCopyInnerJson = (ActionConsumable) Delegate.CreateDelegate(typeof(ActionConsumable), __instance, OnCopyInnerJsonMethod);

        __instance.ClearComposers();
        ElementBounds elementBounds1 = ElementBounds.Fixed(0.0, 22.0, 500.0, 20.0);
        ElementBounds elementBounds2 = ElementBounds.Fixed(0.0, 11.0, 230.0, 30.0);
        ElementBounds bounds1 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bounds1.BothSizing = ElementSizing.FitToChildren;
        ElementBounds bounds2 = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftTop).WithFixedAlignmentOffset(110.0 + GuiStyle.DialogToScreenPadding, GuiStyle.DialogToScreenPadding);
        // RKN: All this just to update fixedHeight
        ElementBounds bounds3 = ElementBounds.Fixed(-320.0, 35.0, 300.0, 800.0);
        ElementBounds bounds4 = ElementBounds.FixedSize(500.0, 200.0);
        ElementBounds refBounds1 = ElementBounds.FixedSize(500.0, 200.0);
        ElementBounds elementBounds3 = ElementBounds.FixedSize(200.0, 20.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0);
        List<GuiTab> source = new List<GuiTab>()
        {
          new GuiTab()
          {
            DataInt = 0,
            Name = Lang.Get("transform-guiTransform")
          },
          new GuiTab()
          {
            DataInt = 2,
            Name = Lang.Get("transform-tpHandTransform")
          },
          new GuiTab()
          {
            DataInt = 3,
            Name = Lang.Get("transform-tpOffHandTransform")
          },
          new GuiTab()
          {
            DataInt = 4,
            Name = Lang.Get("transform-groundTransform")
          }
        };
        int num1 = 5;
        double num2 = GuiElement.scaled(15.0);
        foreach (TransformConfig extraTransform in GuiDialogTransformEditor.extraTransforms)
        {
          source.Add(new GuiTab()
          {
            DataInt = num1++,
            Name = extraTransform.Title,
            PaddingTop = num2
          });
          num2 = 0.0;
        }
        ElementBounds elementBounds4;
        ElementBounds elementBounds5;
        ElementBounds elementBounds6;
        ElementBounds elementBounds7;
        ElementBounds elementBounds8;
        ElementBounds elementBounds9;
        ElementBounds elementBounds10;
        ElementBounds elementBounds11;
        ElementBounds elementBounds12;
        ElementBounds elementBounds13;
        ElementBounds elementBounds14;
        ElementBounds elementBounds15;
        ElementBounds elementBounds16;
        ElementBounds elementBounds17;
        ElementBounds refBounds2;
        ElementBounds elementBounds18;
        ElementBounds elementBounds19;
        ElementBounds elementBounds20;
        ElementBounds elementBounds21;
        __instance.SingleComposer = ___capi.Gui.CreateCompo("transformeditor", bounds2)
            .AddShadedDialogBG(bounds1)
            .AddDialogTitleBar($"Transform Editor ({___target.ToString()})", OnTitleBarClose)
            .BeginChildElements(bounds1)
            .AddVerticalTabs(source.ToArray(), bounds3, OnTabClicked, "verticalTabs")
            .AddStaticText("Translation X", CairoFont.WhiteDetailText(), elementBounds4 = elementBounds1.FlatCopy().WithFixedWidth(230.0))
            .AddNumberInput(elementBounds5 = elementBounds2.BelowCopy(), OnTranslateX, CairoFont.WhiteDetailText(), "translatex")
            .AddStaticText("Origin X", CairoFont.WhiteDetailText(), elementBounds4.RightCopy(40.0))
            .AddNumberInput(elementBounds5.RightCopy(40.0), OnOriginX, CairoFont.WhiteDetailText(), "originx")
            .AddStaticText("Translation Y", CairoFont.WhiteDetailText(), elementBounds6 = elementBounds4.BelowCopy(fixedDeltaY: 33.0))
            .AddNumberInput(elementBounds7 = elementBounds5.BelowCopy(fixedDeltaY: 22.0), OnTranslateY, CairoFont.WhiteDetailText(), "translatey")
            .AddStaticText("Origin Y", CairoFont.WhiteDetailText(), elementBounds6.RightCopy(40.0))
            .AddNumberInput(elementBounds7.RightCopy(40.0), OnOriginY, CairoFont.WhiteDetailText(), "originy")
            .AddStaticText("Translation Z", CairoFont.WhiteDetailText(), elementBounds8 = elementBounds6.BelowCopy(fixedDeltaY: 32.0))
            .AddNumberInput(elementBounds9 = elementBounds7.BelowCopy(fixedDeltaY: 22.0), OnTranslateZ, CairoFont.WhiteDetailText(), "translatez")
            .AddStaticText("Origin Z", CairoFont.WhiteDetailText(), elementBounds8.RightCopy(40.0))
            .AddNumberInput(elementBounds9.RightCopy(40.0), OnOriginZ, CairoFont.WhiteDetailText(), "originz")
            .AddStaticText("Rotation X", CairoFont.WhiteDetailText(), elementBounds10 = elementBounds8.BelowCopy(fixedDeltaY: 33.0).WithFixedWidth(500.0))
            .AddSlider(OnRotateX, elementBounds11 = elementBounds9.BelowCopy(fixedDeltaY: 22.0).WithFixedWidth(500.0), "rotatex")
            .AddStaticText("Rotation Y", CairoFont.WhiteDetailText(), elementBounds12 = elementBounds10.BelowCopy(fixedDeltaY: 32.0))
            .AddSlider(OnRotateY, elementBounds13 = elementBounds11.BelowCopy(fixedDeltaY: 22.0), "rotatey")
            .AddStaticText("Rotation Z", CairoFont.WhiteDetailText(), elementBounds14 = elementBounds12.BelowCopy(fixedDeltaY: 32.0))
            .AddSlider(OnRotateZ, elementBounds15 = elementBounds13.BelowCopy(fixedDeltaY: 22.0), "rotatez")
            .AddStaticText("Scale", CairoFont.WhiteDetailText(), elementBounds16 = elementBounds14.BelowCopy(fixedDeltaY: 32.0))
            .AddSlider(OnScale, elementBounds17 = elementBounds15.BelowCopy(fixedDeltaY: 22.0), "scale")
            .AddSwitch(onFlipXAxis, refBounds2 = elementBounds17.BelowCopy(fixedDeltaY: 10.0), "flipx", 20.0)
            .AddStaticText("Flip on X-Axis", CairoFont.WhiteDetailText(), refBounds2.RightCopy(10.0, 1.0).WithFixedWidth(200.0))
            .AddSwitch(onFlipByTypeJson, refBounds2.RightCopy(120.0), "bytypeswitch", 20.0)
            .AddStaticText("*byType json output (+Bulk editing)", CairoFont.WhiteDetailText(), refBounds2.RightCopy(150.0, 1.0).WithFixedWidth(300.0))
            .AddStaticText("Json Code", CairoFont.WhiteDetailText(), elementBounds16.BelowCopy(fixedDeltaY: 72.0))
            .BeginClip(refBounds1.FixedUnder(refBounds2, 37.0))
            .AddTextArea(bounds4, null, CairoFont.WhiteSmallText(), "textarea")
            .EndClip()
            .AddButton("Apply & Next", OnNextSlot, elementBounds18 = elementBounds3.FlatCopy().FixedUnder(refBounds1, 15.0).WithFixedWidth(130.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(5.0, 2.0), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), EnumButtonStyle.Small, "apply")
            .AddButton("Reset JSON", OnResetJson, elementBounds19 = elementBounds18.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(0.0, 0.0).WithFixedPadding(5.0, 2.0), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), EnumButtonStyle.Small, "resetjson")
            .AddSmallButton("Close & Apply", OnApplyJson, elementBounds20 = elementBounds19.BelowCopy(fixedDeltaY: 10.0).WithFixedWidth(200.0).WithAlignment(EnumDialogArea.LeftFixed))
            .AddSmallButton("Copy Full JSON", OnCopyJson, elementBounds21 = elementBounds20.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
            .AddButton("Copy inner JSON", OnCopyInnerJson, elementBounds21.BelowCopy(fixedDeltaY: 3.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedWidth(100.0).WithFixedPadding(5.0, 2.0), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), EnumButtonStyle.Small, "copyinner")
            .EndChildElements()
            .Compose();
        __instance.SingleComposer.GetTextInput("translatex").SetValue(___currentTransform.Translation.X.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetTextInput("translatey").SetValue(___currentTransform.Translation.Y.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetTextInput("translatez").SetValue(___currentTransform.Translation.Z.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetTextInput("originx").SetValue(___currentTransform.Origin.X.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetTextInput("originy").SetValue(___currentTransform.Origin.Y.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetTextInput("originz").SetValue(___currentTransform.Origin.Z.ToString(GlobalConstants.DefaultCultureInfo));
        __instance.SingleComposer.GetSlider("rotatex").SetValues((int) ___currentTransform.Rotation.X, -180, 180, 1);
        __instance.SingleComposer.GetSlider("rotatey").SetValues((int) ___currentTransform.Rotation.Y, -180, 180, 1);
        __instance.SingleComposer.GetSlider("rotatez").SetValues((int) ___currentTransform.Rotation.Z, -180, 180, 1);
        __instance.SingleComposer.GetSlider("scale").SetValues((int) Math.Abs(100f * ___currentTransform.ScaleXYZ.X), 25, 600, 1);
        __instance.SingleComposer.GetSwitch("flipx").On = (double) ___currentTransform.ScaleXYZ.X < 0.0;
        __instance.SingleComposer.GetSwitch("bytypeswitch").On = ___byTypeJson;
        __instance.SingleComposer.GetVerticalTab("verticalTabs").SetValue(source.IndexOf<GuiTab>((ActionBoolReturn<GuiTab>) (tab => tab.DataInt == ___target)), false);
        __instance.SingleComposer.GetButton("apply").Enabled = ___byTypeJson;
        __instance.SingleComposer.GetButton("copyinner").Enabled = !___byTypeJson;
        __instance.SingleComposer.GetButton("resetjson").Enabled = ___byTypeJson;
        
        return false;
    }
}