using System;
using RKN.Crafting;
using RKN.Crafting.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace RknCrafting;

internal class RecipeSelectionDialog(ICoreClientAPI capi) : GuiDialog(capi)
{
    private IInventory recipeInventory;
    private ActionConsumable<KeyCombination>? defaultHotKeyHandler;
    private BlockEntityCraftingSurface entity;
    public override double DrawOrder => 0.09f; // Little less than GuiDialogToolMode so we can replace its handler
    public override string ToggleKeyCombinationCode => "toolmodeselect";
    
    /*private readonly double floatyDialogPosition = 0.5;
    private readonly double floatyDialogAlign = 0.75;*/

    public override void OnBlockTexturesLoaded()
    {
        defaultHotKeyHandler = capi.Input.HotKeys[ToggleKeyCombinationCode]?.Handler;
        capi.Input.SetHotKeyHandler(ToggleKeyCombinationCode, TryToggle);
    }

    private bool TryToggle(KeyCombination keys)
    {
        bool handled = IsOpened() ? TryClose() : TryOpen();
        if (!handled && defaultHotKeyHandler != null)
        {
            return defaultHotKeyHandler(keys);
        }
        return handled;
    }

    public override bool TryOpen()
    {
        BlockSelection sel = capi.World.Player.Entity.BlockSelection;
        if (sel?.Block is not BlockCraftingSurface)
        {
            return false;
        }
        
        BlockEntityCraftingSurface? be = BlockCraftingSurface.GetBE(capi.World, sel.Position);
        if (be == null)
        {
            return false;
        }

        entity = be;
        ICraftingResult[] recipes = entity.ValidRecipes?.ToArray() ?? [];
        recipeInventory = new RecipeSelectionInventory(capi, recipes, i =>
        {
            entity.SetRecipe(recipes[i].Id);
            TryClose();
        });
        return base.TryOpen();
    }

    public override void OnGuiOpened()
    {
        ComposeDialog();
    }

    private void ComposeDialog()
    {
        ClearComposers();
        double dialogPadding = GuiStyle.ElementToDialogPadding;
        double unscaledSlotPadding = GuiElementItemSlotGridBase.unscaledSlotPadding;
        int rows = (int)Math.Ceiling(recipeInventory.Count / 6f);
        ElementBounds elementBounds = ElementStdBounds
            .SlotGrid(EnumDialogArea.None, unscaledSlotPadding, unscaledSlotPadding, 6, 2)
            .FixedGrow(2.0 * unscaledSlotPadding, 2.0 * unscaledSlotPadding);
        ElementBounds inventoryBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0.0, 0.0, 6, rows);
        ElementBounds insetBounds = elementBounds.ForkBoundingParent(3.0, 3.0, 3.0, 3.0);
        ElementBounds clipBounds = elementBounds.CopyOffsetedSibling();
        clipBounds.fixedHeight -= 3.0;
        double countWidth = 60;
        ElementBounds countBounds = new()
        {
            Alignment = EnumDialogArea.LeftTop,
            BothSizing = ElementSizing.Fixed,
            fixedHeight = 90,
            fixedWidth = countWidth,
            fixedX = dialogPadding,
            fixedY = dialogPadding + 30.0,
            /*fixedPaddingX = 2.0,
            fixedPaddingY = 2.0*/
        };
        ElementBounds compoBounds = insetBounds.ForkBoundingParent(dialogPadding + 10 + countWidth, dialogPadding + 30.0, dialogPadding + 20.0, dialogPadding);
        compoBounds.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(20.0, 0.0);
        ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(compoBounds);
        scrollbarBounds.fixedOffsetX -= 2.0;
        scrollbarBounds.fixedWidth = 15.0;
        SingleComposer = capi.Gui.CreateCompo("inventory-recipes", compoBounds)
            .AddShadedDialogBG(ElementBounds.Fill)
            .AddDialogTitleBar("Select Recipe", CloseIconPressed)
            .AddInset(countBounds, 1)
            .BeginChildElements()
            .AddStaticText("Limit", CairoFont.WhiteSmallText(), ElementBounds.Fixed(EnumDialogArea.CenterTop, 0, 0, countWidth, 30))
            .AddNumberInput(ElementBounds.Fixed(EnumDialogArea.CenterMiddle, 0, 0, countWidth, 30), OnCountChanged, key: "limit")
            .AddSmallButton("Reset", OnLimitReset, ElementBounds.Fixed(EnumDialogArea.CenterBottom, 0, 0, countWidth, 30))
            .EndChildElements()
            .AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
            .AddInset(insetBounds, 3)
            .BeginClip(clipBounds)
            .AddItemSlotGrid(recipeInventory, null, 6, inventoryBounds, "slotgrid")
            .EndClip()
            .Compose();
        SingleComposer.GetNumberInput("limit").SetValue(entity.SelectedLimit);
        SingleComposer.GetScrollbar("scrollbar").SetHeights((float)elementBounds.fixedHeight, (float)(inventoryBounds.fixedHeight + unscaledSlotPadding));
    }

    private bool OnLimitReset()
    {
        SingleComposer.GetNumberInput("limit").SetValue(0);
        return true;
    }

    private void OnCountChanged(string _)
    {
        var input = SingleComposer.GetNumberInput("limit");
        float v = input.GetValue();
        if (v < 0)
        {
            v = 0;
        }
        else if (v != Math.Floor(v))
        {
            v = (float) Math.Floor(v);
        }
        // TODO: doesn't work
        //input.Text = v.ToString();
        entity.SetLimit((int)v);
    }
    
    private void CloseIconPressed() => TryClose();

    private void OnNewScrollbarvalue(float value)
    {
        if (!IsOpened())
            return;
        ElementBounds bounds = SingleComposer.GetSlotGrid("slotgrid").Bounds;
        bounds.fixedY = 10.0 - GuiElementItemSlotGridBase.unscaledSlotPadding - value;
        bounds.CalcWorldBounds();
    }
    
    /*public override void OnRenderGUI(float deltaTime)
    {
        if (capi.Settings.Bool["immersiveMouseMode"])
        {
            Vec3d vec3d = MatrixToolsd.Project(new Vec3d(pos.X + 0.5, pos.Y + floatyDialogPosition, pos.Z + 0.5), capi.Render.PerspectiveProjectionMat, capi.Render.PerspectiveViewMat, capi.Render.FrameWidth, capi.Render.FrameHeight);
            if (vec3d.Z < 0.0)
                return;
            SingleComposer.Bounds.Alignment = EnumDialogArea.None;
            SingleComposer.Bounds.fixedOffsetX = 0.0;
            SingleComposer.Bounds.fixedOffsetY = 0.0;
            SingleComposer.Bounds.absFixedX = vec3d.X - SingleComposer.Bounds.OuterWidth / 2.0;
            SingleComposer.Bounds.absFixedY = capi.Render.FrameHeight - vec3d.Y - SingleComposer.Bounds.OuterHeight * floatyDialogAlign;
            SingleComposer.Bounds.absMarginX = 0.0;
            SingleComposer.Bounds.absMarginY = 0.0;
        }
        base.OnRenderGUI(deltaTime);
    }*/
    
    private  class RecipeSelectionInventory : InventoryBase
    {
        private ItemSlot[] slots;
        private Action<int> selected;

        public RecipeSelectionInventory(ICoreAPI api, ICraftingResult[] recipes, Action<int> selected) : base("recipeSelection", "0", api)
        {
            slots = GenEmptySlots(recipes.Length);
            this.selected = selected;
            for (var index = 0; index < recipes.Length; index++)
            {
                var wrapper = recipes[index];
                ItemStack itemStack = wrapper.SelectionItemStack;
                slots[index].Itemstack = itemStack;
            }
        }

        public override ItemSlot this[int slotId] { get => slots[slotId]; set => throw new NotImplementedException(); }

        public override int Count => slots.Length;

        public override object? ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            selected(slotId);
            return null;
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            throw new NotImplementedException();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            throw new NotImplementedException();
        }
    }
}