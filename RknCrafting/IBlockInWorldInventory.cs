using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RknCrafting;

public interface IBlockInWorldInventory
{
    ItemSlot? GetItemSlot(BlockSelection blockSelection, ItemSlot mouseSlot, MouseEvent args, out int slotId);
    InventoryBase? GetInventory(BlockSelection blockSelection);
    void OnModified(BlockSelection blockSelection, IPlayer? byPlayer);
}