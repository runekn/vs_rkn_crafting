using RKN.Crafting;
using RKN.Crafting.Entities;
using Vintagestory.API.Common;

namespace RknCrafting.Entities;

public class BlockEntityBehaviorCraftingSurfaceMouseSlotRecipient(BlockEntity be)
    : BlockEntityBehavior(be), IBlockMouseSlotRecipient
{
    public bool OnClick(ItemSlot slot, ref ItemStackMoveOperation op, BlockSelection blockSelection)
    {
        if (Blockentity is not BlockEntityCraftingSurface be)
        {
            return false;
        }

        if (slot.Empty)
        {
            ItemSlot? invSlot = be.GetInventorySlotForTaking(slot, blockSelection.SelectionBoxIndex);
            if (invSlot == null)
            {
                Api.RcTriggerIngameError(this, "surfaceempty");
                return true;
            }

            // TODO: Shift + right click should move all directly to player inv
            if (op.MouseButton == EnumMouseButton.Right)
            {
                op.RequestedQuantity = invSlot.StackSize / 2;
            }
            else
            {
                op.RequestedQuantity = invSlot.StackSize;
            }
            be.TryTakeIngredient(slot, ref op, invSlot);
        }
        else
        {
            ItemSlot? invSlot = be.GetInventorySlotForPutting(slot, blockSelection.SelectionBoxIndex);
            if (invSlot == null)
            {
                Api.RcTriggerIngameError(this, "surfacefull");
                return true;
            }

            if (op.MouseButton == EnumMouseButton.Left)
            {
                op.RequestedQuantity = slot.StackSize;
            }
            be.TryPutIngredient(slot, ref op, invSlot);
        }

        return true;
    }
}