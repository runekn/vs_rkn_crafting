using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RKN.GridlessCrafting;

public class BlockBehaviorCraftingSurface : BlockBehavior
{
    public BlockBehaviorCraftingSurface(Block block) : base(block)
    {
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        return true;
        /*ICoreClientAPI? clientApi = world.Api as ICoreClientAPI;
        if (clientApi != null && clientApi.Input.IsHotKeyPressed("rkngridlesscrafting.start"))
        {
            (clientApi.World.GetBlock(new AssetLocation("rkngridlesscrafting:crafting")) as BlockCrafting).TryPlace(byEntity, blockSel, slot);
            handling = EnumHandling.PreventSubsequent;
        }
        return true;*/
    }
}