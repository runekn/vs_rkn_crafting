using System.Linq;
using RKN.Crafting;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace RknCrafting;

public class HudMouseSlotInteract : HudElement
{
    public override double DrawOrder => 1.01; // Little larger than HudDropItem

    public HudMouseSlotInteract(ICoreClientAPI capi)
        : base(capi)
    {
        TryOpen();
    }

    public override bool TryClose() => false;

    public override void OnMouseDown(MouseEvent args)
    {
        if (args.Handled)
            return;
        if (capi.Gui.OpenedGuis
            .Where(openedGui => openedGui.IsOpened() && openedGui is not HudMouseTools)
            .SelectMany(openedGui => openedGui.Composers.Values)
            .Any(guiComposer => guiComposer.Bounds.PointInside(args.X, args.Y)))
        {
            return;
        }

        BlockSelection? blockSelection = capi.World.Player.CurrentBlockSelection;
        ItemSlot slot = capi.World.Player.InventoryManager.MouseItemSlot;
        if (blockSelection == null)
        {
            return;
        }
        IBlockMouseSlotRecipient? blockMouseSlotRecipient = capi.World.BlockAccessor.GetBlock(blockSelection.Position)?.GetInterface<IBlockMouseSlotRecipient>(capi.World, blockSelection.Position);
        if (blockMouseSlotRecipient == null)
        {
            return;
        }

        bool shift = capi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || capi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
        bool ctrl = capi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft];
        bool alt = capi.Input.KeyboardKeyState[(int)GlKeys.AltLeft];
        EnumModifierKey modifiers = shift ? EnumModifierKey.SHIFT : ctrl ? EnumModifierKey.CTRL : alt ?  EnumModifierKey.ALT : 0;
        ItemStackMoveOperation op = new(capi.World, args.Button, modifiers, EnumMergePriority.AutoMerge, 1)
        {
            ActingPlayer = capi.World.Player
        };
        args.Handled = blockMouseSlotRecipient.OnClick(slot, ref op, blockSelection);
        if (args.Handled)
        {
            capi.RcNetwork().BlockMouseInteraction(op, blockSelection);
        }
    }

    public override bool ShouldReceiveKeyboardEvents() => true;

    public override bool Focusable => false;
}