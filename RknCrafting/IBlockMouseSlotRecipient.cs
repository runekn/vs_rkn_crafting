using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RknCrafting;

public interface IBlockMouseSlotRecipient
{
    bool OnClick(ItemSlot slot, ref ItemStackMoveOperation op, BlockSelection blockSelection);
}