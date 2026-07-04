using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RKN.GridlessCrafting;

public class ItemCraftingWand : Item
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (blockSel != null)
        {
            JsonObject attributes = api.World.BlockAccessor.GetBlock(blockSel.Position).Attributes;
            if (attributes != null && attributes.IsTrue("craftingSurface"))
            {
                (api.World.GetBlock(new AssetLocation("rkngridlesscrafting:crafting")) as BlockCrafting).TryPlace(byEntity, blockSel, slot);
                handling = EnumHandHandling.PreventDefault;
                return;
            }
        }
        base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }
}