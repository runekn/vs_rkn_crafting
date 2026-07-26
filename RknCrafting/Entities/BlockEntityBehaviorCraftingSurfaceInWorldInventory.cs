using RKN.Crafting;
using RKN.Crafting.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RknCrafting.Entities;

public class BlockEntityBehaviorCraftingSurfaceInWorldInventory(BlockEntity be)
    : BlockEntityBehavior(be), IBlockInWorldInventory
{
    public ItemSlot? GetItemSlot(BlockSelection blockSelection, ItemSlot mouseSlot, MouseEvent args, out int slotId)
    {
        slotId = 0;
        if (Blockentity is not BlockEntityCraftingSurface be)
        {
            return null;
        }

        args.Handled = true;
        if (mouseSlot.Empty)
        {
            ItemSlot? invSlot = be.GetInventorySlotForTaking(mouseSlot, out slotId, blockSelection.SelectionBoxIndex);
            if (invSlot != null)
                return invSlot;
            Api.RcTriggerIngameError(this, "surfaceempty");
        }
        else
        {
            ItemSlot? invSlot = be.GetInventorySlotForPutting(mouseSlot, out slotId, blockSelection.SelectionBoxIndex);
            if (invSlot != null)
                return invSlot;
            Api.RcTriggerIngameError(this, "surfacefull");
        }

        return null;
    }

    public InventoryBase? GetInventory(BlockSelection blockSelection)
    {
        return (Blockentity as BlockEntityCraftingSurface)?.Inventory;
    }

    public void OnModified(BlockSelection blockSelection, IPlayer? byPlayer)
    {
        (Blockentity as BlockEntityCraftingSurface)?.MarkIngredientsDirty(byPlayer);
    }
}