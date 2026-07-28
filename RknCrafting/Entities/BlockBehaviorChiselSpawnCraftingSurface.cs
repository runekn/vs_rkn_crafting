using RKN.Crafting.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RknCrafting.Entities;

public class BlockBehaviorChiselSpawnCraftingSurface(Block block) : BlockBehaviorSpawnCraftingSurface(block)
{
    public override float GetCraftingModifier(IWorldAccessor world, BlockPos pos)
    {
        BlockEntityMicroBlock entity = world.BlockAccessor.GetBlockEntity<BlockEntityMicroBlock>(pos);
        return world.GetBlock(entity.BlockIds[0]).GetBehavior<BlockBehaviorSpawnCraftingSurface>()?.GetCraftingModifier(world, pos) ?? 1f;
    }
}