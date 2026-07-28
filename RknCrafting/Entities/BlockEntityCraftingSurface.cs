using RKN.Crafting.Animation;
using RknCrafting;
using RknCrafting.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RKN.Crafting.Entities;

public class BlockEntityCraftingSurface : BlockEntityDisplay
{
    // Initialized fields
    private int slotCount = 9;
    private InventoryGeneric inventory;
    private RknCraftingConfig config;
    private float craftingSurfaceTimeModifier = 1.0f;

    public override InventoryBase Inventory => inventory;
    public override string InventoryClassName => "craftingsurface";
    public override string AttributeTransformCode => "craftingIngredientTransform";

    // Client Runtime fields
    private int selectedRecipe = RecipeService.RecipeIdNone;
    private List<ICraftingResult>? validRecipes;
    private BlockFacing? lastFacing;
    private EnumTool? lastTool;
    private bool dirtyRecipes = true;
    private RecipeSelectionDialog? recipeSelectionDialog;

    // Server-client Runtime fields
    private float timeoutTimer;
    private CraftingParams? craftingParams;

    public BlockEntityCraftingSurface()
    {
        inventory = new CraftingInventory(slotCount, null, this);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        config = api.RcServerConfig();
        craftingParams = null; // Override FromTreeAttributes because we don't want dummy craftingParams on server
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        base.OnTesselation(mesher, tessThreadTesselator);
        return true; // Prevent default cube from being rendered
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        // FYI: GetBlockInfo Is called every 500 milliseconds it seems.
        //base.GetBlockInfo(forPlayer, sb); // Just food perish stuff
        bool gridless = Api.RcServerConfig().EnableGridless;
        int selectedIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        for (int index = 0; index < inventory.Count; index++)
        {
            ItemSlot itemSlot = inventory[index];
            if (itemSlot.Empty)
            {
                continue;
            }

            sb.Append(itemSlot.Itemstack.GetName());
            if (itemSlot.Itemstack.StackSize > 1)
            {
                sb.Append(" x");
                sb.Append(itemSlot.Itemstack.StackSize);
            }

            if (!gridless && selectedIndex == index)
            {
                sb.Append(" --");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        var scanResult = GetSelectedRecipe();
        if (scanResult == null)
        {
            return;
        }
        sb.Append("Selected: ").AppendLine(scanResult.SelectionItemStack.GetName());
        if (validRecipes is { Count: > 1 })
        {
            sb.Append(validRecipes.Count - 1).Append(" more valid recipes");
        }
    }

    protected override MeshData getOrCreateMesh(ItemSlot slot, int index)
    {
        // Fix crates. Because they do not have proper config to display their custom mesh
        if (CraftingItemRenderer.NeedsCustomMesh(slot.Itemstack?.Block))
        {
            MeshData mesh = getMesh(slot);
            if (mesh != null)
                return mesh;
            ItemStack stack = slot.Itemstack!;
            mesh = CraftingItemRenderer.GenCustomMesh(capi, stack);
            applyDefaultTranforms(stack, mesh);
            MeshCache[getMeshCacheKey(slot)] = mesh; // This means blocks whose shape are dependent on some internal attribute will not update for different itemstacks of same id. But if I don't cache then it doesn't work at all.
            return mesh;
        }
        return base.getOrCreateMesh(slot, index);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        timeoutTimer = tree.GetFloat("timeoutTimer");
        if (tree.GetBool("isCrafting"))
        {
            craftingParams ??= new CraftingParams()
            {
                OtherPlayer = true
            };
        }
        else if (craftingParams?.OtherPlayer == true)
        {
            craftingParams = null;
        }
        dirtyRecipes = true;
        if (recipeSelectionDialog != null && recipeSelectionDialog.IsOpened())
        {
            recipeSelectionDialog.TryClose();
        }
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        tree.SetFloat("timeoutTimer", timeoutTimer);
        tree.SetBool("isCrafting", craftingParams != null);
    }

    protected override float[][] genTransformationMatrices()
    {
        return CraftingItemRenderer.GenTransformationMatrices(inventory, AttributeTransformCode, config.EnableGridless, Block, getMesh);
    }

    public bool IsCrafting(IPlayer byPlayer)
    {
        return craftingParams?.Player.ClientId == byPlayer.ClientId;
    }

    public void ClientStartedCrafting(IPlayer byPlayer, EnumCraftingAnimation animation, float recipeModifier, int recipe, bool bulk, float nextCraftingTime, BlockFacing? blockFacing)
    {
        RecipeInputSlots? inputSlots = GetCraftingInputSlots(byPlayer, blockFacing);
        if (inputSlots == null)
        {
            Api.RcLogger().Error("Player {0} started crafting on client but there are no ingredients on server!", byPlayer.PlayerName);
            return;
        }
        ICraftingResult? scanResult = Api.RcRecipeService().GetRecipe(recipe, inputSlots);
        if (scanResult == null)
        {
            Api.RcLogger().Error("Could not match recipe on server side!");
            return;
        }

        UpdateCraftingSurfaceModifier();

        craftingParams = new CraftingParams()
        {
            Player = byPlayer,
            Recipe = scanResult,
            Animation = animation,
            Bulk = bulk,
            RecipeCraftingTimeModifier = recipeModifier,
            NextCraftingTime = nextCraftingTime,
            Facing = blockFacing
        };
        Api.RcAnimator().StartCrafting(byPlayer, animation);
    }

    public bool StartCrafting(IPlayer byPlayer)
    {
        timeoutTimer = 0;
        if (Api.Side == EnumAppSide.Server)
        {
            return true;
        }

        if (craftingParams != null)
        {
            if (!IsCrafting(byPlayer))
            {
                Api.RcTriggerIngameError(this, "surfacealreadycrafting");
            }
            return false;
        }
        ICraftingResult? result = GetSelectedRecipe();
        if (result == null)
        {
            return false;
        }
        RecipeInputSlots? inputSlots = GetCraftingInputSlots(byPlayer, lastFacing);
        if (inputSlots == null || !result.Matches(inputSlots))
        {
            Api.RcTriggerIngameError(this, "missingreciperequirement");
            return false;
        }
        bool bulk = byPlayer.Entity.Controls.ShiftKey && Api.RcServerConfig().EnableBulkCrafting;
        
        UpdateCraftingSurfaceModifier();
        
        craftingParams = new CraftingParams()
        {
            Player = byPlayer,
            Facing = lastFacing,
            Recipe = result,
            Animation = Api.RcAnimator().StartCrafting(byPlayer, selectedRecipe, inputSlots.PrimaryTool, inputSlots.OffhandTool),
            Bulk = bulk,
            RecipeCraftingTimeModifier = result.CraftingTimeModifier,
        };
        craftingParams.NextCraftingTime = GetCraftingTime();
        Api.RcNetwork().ClientStartedCrafting(craftingParams, Pos);
        Api.RcLogger().Debug("Started crafting {0} by {1}!", craftingParams.Recipe.Name, craftingParams.Player.PlayerName);
        return true;
    }

    public bool OnCraftingStep(float secondsUsed, IPlayer byPlayer)
    {
        timeoutTimer = 0;
        if (Api.Side != EnumAppSide.Server || craftingParams == null)
        {
            return true;
        }
        if (craftingParams.Bulk && !byPlayer.Entity.Controls.ShiftKey)
        {
            // Player let go of bulk modifier key
            EnumCraftingAnimation enumCraftingAnimation = GetCraftingAnimation();
            Api.RcNetwork().StopCrafting(craftingParams.Player, enumCraftingAnimation, Pos);
            Api.RcAnimator().StopCrafting(craftingParams.Player, enumCraftingAnimation);
            ResetState();
            return false;
        }
        if (secondsUsed > craftingParams.NextCraftingTime && IsCrafting(byPlayer))
        {
            craftingParams.Amount++;
            craftingParams.NextCraftingTime = secondsUsed + GetCraftingTime();

            CreateOutput();

            // Continue crafting if possible
            RecipeInputSlots inputSlots = GetCraftingInputSlots(craftingParams.Player, craftingParams.Facing);
            if (inputSlots == null)
            {
                Api.RcLogger().Debug("Stopping crafting by {0} and destroying block due to lack of any materials", byPlayer.PlayerName);
                Api.World.BlockAccessor.BreakBlock(Pos, byPlayer);
                EnumCraftingAnimation enumCraftingAnimation = GetCraftingAnimation();
                Api.RcNetwork().StopCrafting(craftingParams.Player, enumCraftingAnimation, Pos);
                Api.RcAnimator().StopCrafting(craftingParams.Player, enumCraftingAnimation);
                return false;
            }
            if (!craftingParams.Recipe.Matches(inputSlots))
            {
                Api.RcLogger().Debug("Stopping crafting by {0} due to lack of correct materials", byPlayer.PlayerName);
                EnumCraftingAnimation enumCraftingAnimation = GetCraftingAnimation();
                Api.RcNetwork().StopCrafting(craftingParams.Player, enumCraftingAnimation, Pos);
                Api.RcAnimator().StopCrafting(craftingParams.Player, enumCraftingAnimation);
                ResetState();
                return false;
            }
            MarkDirty(true);
        }

        return true;
    }

    public void CancelCrafting(IPlayer byPlayer)
    {
        timeoutTimer = 0;
        if (craftingParams?.Player?.ClientId != byPlayer.ClientId)
        {
            return;
        }
        Api.RcLogger().Debug("Cancelled crafting by {0}!", craftingParams.Player.PlayerName);
        EnumCraftingAnimation anim = GetCraftingAnimation();
        Api.RcAnimator().StopCrafting(byPlayer, anim);
        ResetState();
    }

    public void ClientStopCrafting(EnumCraftingAnimation anim)
    {
        timeoutTimer = 0;
        IPlayer player = capi.World.Player;
        capi.RcPauseInteractions();
        Api.RcAnimator().StopCrafting(player, anim);
        ResetState();
    }

    private EnumCraftingAnimation GetCraftingAnimation()
    {
        return craftingParams?.Animation ?? EnumCraftingAnimation.HandsGeneric;
    }

    public ItemSlot? GetInventorySlotForTaking(ItemSlot slot, out int slotId, int selectionBoxIndex = 0)
    {
        return GetInventorySlot(invSlot => slot.CanTakeFrom(invSlot), selectionBoxIndex, out slotId, true);
    }
    
    public ItemSlot? GetInventorySlotForPutting(ItemSlot slot, out int slotId, int selectionBoxIndex = 0)
    {
        return GetInventorySlot(invSlot => invSlot.CanTakeFrom(slot), selectionBoxIndex, out slotId);
    }

    private ItemSlot? GetInventorySlot(Predicate<ItemSlot> test, int selectionBoxIndex, out int slotId, bool reverse = false)
    {
        slotId = 0;
        if (config.EnableGridless)
        {
            IEnumerable<(int, ItemSlot)> enumerable = reverse ? inventory.Index().Reverse() : inventory.Index();
            foreach ((int, ItemSlot) invSlot in enumerable)
            {
                if (test(invSlot.Item2))
                {
                    slotId = invSlot.Item1;
                    return invSlot.Item2;
                }
            }
        }
        else
        {
            ItemSlot invSlot = inventory[selectionBoxIndex];
            if (test(invSlot))
            {
                slotId = selectionBoxIndex;
                return invSlot;
            }
        }
        return null;
    }

    public void OpenRecipeSelection()
    {
        if (validRecipes == null)
        {
            return;
        }
        recipeSelectionDialog ??= new RecipeSelectionDialog(capi, Pos);
        recipeSelectionDialog
            .TryOpen(validRecipes.ToArray(), i =>
            {
                selectedRecipe = i;
                Api.RcLogger().Debug("Selected recipe: {0} {1}", i, GetSelectedRecipe()!.Name);
                recipeSelectionDialog.TryClose();
            });
    }

    public void CheckIfUpdateRecipes(IPlayer byPlayer)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            return;
        }
        if (!config.EnableGridless) {
            BlockFacing blockFacing = GetBlockOrientation(Pos, byPlayer);
            if (blockFacing != lastFacing)
            {
                lastFacing = blockFacing;
                dirtyRecipes = true;
            }
        }
        if (byPlayer.InventoryManager.ActiveTool != lastTool)
        {
            lastTool = byPlayer.InventoryManager.ActiveTool;
            dirtyRecipes = true;
        }

        if (dirtyRecipes && craftingParams == null)
        {
            UpdateValidRecipes(byPlayer);
            dirtyRecipes = false;
        }
    }

    public bool HasRecipeSelection()
    {
        return GetSelectedRecipe() != null;
    }

    protected override void OnTick(float dt)
    {
        base.OnTick(dt);
        timeoutTimer += dt;
        if(timeoutTimer >= config.AutoDeleteTimeSeconds)
        {
            Api.World.BlockAccessor.BreakBlock(Pos, null);
        }
    }

    private float GetCraftingTime()
    {
        float @base = craftingParams!.Bulk ? config.BulkBaseCraftingTimeSeconds : config.BaseCraftingTimeSeconds;
        float consecutiveModifer = craftingParams.Amount == 0 ? 1 : Math.Max(config.ConsecutiveCraftingTimeModifierMin, (float)Math.Pow(config.ConsecutiveCraftingTimeModifier, craftingParams.Amount));
        float r = @base * craftingSurfaceTimeModifier * craftingParams.RecipeCraftingTimeModifier * consecutiveModifer;
        Api.RcLogger().Debug("Next time to craft: {0} * {1} * {2} * {3} = {4}", @base, craftingSurfaceTimeModifier, craftingParams.RecipeCraftingTimeModifier, consecutiveModifer, r);
        return r;
    }

    private void UpdateValidRecipes(IPlayer byPlayer)
    {
        if (Api.Side == EnumAppSide.Server)
        {
            return;
        }
        RecipeInputSlots? inputSlots = GetCraftingInputSlots(byPlayer, lastFacing);
        if (inputSlots == null) {
            validRecipes = [];
            selectedRecipe = RecipeService.RecipeIdNone;
            return;
        }
        List<ICraftingResult> recipes = Api.RcRecipeService().GetValidRecipes(inputSlots);
        validRecipes = recipes;
        if (recipes.Count == 0)
        {
            selectedRecipe = RecipeService.RecipeIdNone;
        }
        else if (GetSelectedRecipe() == null)
        {
            selectedRecipe = validRecipes.First().Id;
        }
    }

    private void CreateOutput()
    {
        if (craftingParams == null)
        {
            return;
        }
        RecipeInputSlots? inputSlots = GetCraftingInputSlots(craftingParams.Player, craftingParams.Facing);
        if (inputSlots?.Items == null || !craftingParams.Recipe.Matches(inputSlots))
        {
            return;
        }

        ItemStack? output = craftingParams.Recipe.GenerateOutput(inputSlots, craftingParams.Bulk);
        if (output == null)
        {
            return;
        }
        Api.World.SpawnCubeParticles(Pos.ToVec3d().Add(0.5f, 0, 0.5f), output, 1f, 6, 1f, null, new Vec3f(0.07f, 0.75f, 0.07f));
        Api.World.SpawnItemEntity(output, Pos);
        Api.RcLogger().Debug("Crafted {0}x {1} by {2}!", output.StackSize, output.GetName(), craftingParams.Player.PlayerName);
    }

    private RecipeInputSlots? GetCraftingInputSlots(IPlayer byPlayer, BlockFacing? facing)
    {
        if (inventory.All(s => s == null || s.StackSize == 0))
        {
            return null;
        }

        IPlayerInventoryManager inventoryManager = byPlayer.InventoryManager;
        ItemSlot? primaryTool = inventoryManager.ActiveTool != null ? inventoryManager.ActiveHotbarSlot : null;
        ItemSlot? offhandTool = inventoryManager.OffhandTool != null ? inventoryManager.OffhandHotbarSlot : null;
        return new RecipeInputSlots(RearrangeGridByFacing(inventory.ToArray(), facing), byPlayer, primaryTool, offhandTool);
    }

    /**
        SOUTH (default)     Resulting indexes
        0 1 2		        012345678
        3 4 5
        6 7 8

        NORTH
        8 7 6		        876543210
        5 4 3
        2 1 0

        WEST
        6 3 0		        630741852
        7 4 1
        8 5 2

        EAST
        2 5 8		        258147036
        1 4 7
        0 3 6
     */

    private ItemSlot[] RearrangeGridByFacing(ItemSlot[] a, BlockFacing? facing)
    {
        if (facing == null || config.EnableGridless || facing == BlockFacing.SOUTH)
        {
            return a;
        }
        if (facing == BlockFacing.NORTH)
        {
            return [a[8], a[7], a[6], a[5], a[4], a[3], a[2], a[1], a[0]];
        }
        if (facing == BlockFacing.EAST)
        {
            return [a[6], a[3], a[0], a[7], a[4], a[1], a[8], a[5], a[2]];
        }
        else // WEST
        {
            return [a[2], a[5], a[8], a[1], a[4], a[7], a[0], a[3], a[6]];
        }
    }

    private static BlockFacing GetBlockOrientation(BlockPos blockPos, IPlayer? byPlayer)
    {
        if (byPlayer == null)
        {
            return BlockFacing.NORTH;
        }

        EntityPos playerPos = byPlayer.Entity.Pos;
        Vec3f facingVector = new Vec3f((float)playerPos.X - (blockPos.X + 0.5f), 0, (float)playerPos.Z - (blockPos.Z + 0.5f)).Normalize();
        if (facingVector.Z > Math.Abs(facingVector.X))
        {
            return BlockFacing.SOUTH;
        }
        else if (-facingVector.Z > Math.Abs(facingVector.X))
        {
            return BlockFacing.NORTH;
        }
        else if (facingVector.X > Math.Abs(facingVector.Z))
        {
            return BlockFacing.EAST;
        }
        else
        {
            return BlockFacing.WEST;
        }
    }

    private void ResetState()
    {
        craftingParams = null;
        dirtyRecipes = true;
        MarkDirty();
    }

    private ICraftingResult? GetSelectedRecipe()
    {
        if (validRecipes == null || selectedRecipe == RecipeService.RecipeIdNone || validRecipes.Count == 0)
            return null;

        return validRecipes.FirstOrDefault(r => r.Id == selectedRecipe);
    }

    public bool IsEmpty()
    {
        return inventory.Empty;
    }

    private void UpdateCraftingSurfaceModifier()
    {
        BlockPos down = Pos.DownCopy();
        craftingSurfaceTimeModifier = Api.World.BlockAccessor.GetBlock(down).GetBehavior<BlockBehaviorSpawnCraftingSurface>().GetCraftingModifier(Api.World, down);
    }

    public void MarkIngredientsDirty(IPlayer? byPlayer)
    {
        if (IsEmpty())
        {
            Api.World.BlockAccessor.BreakBlock(Pos, byPlayer);
        }
        //dirtyRecipes = true; // For some reason byPlayer still gets new tree attributes. Commented out to prevent double scan.
        MarkMeshesDirty();
        MarkDirty(true, byPlayer);
    }

    private class CraftingInventory(int quantitySlots, ICoreAPI api, BlockEntityCraftingSurface be)
        : InventoryGeneric(quantitySlots, "craftignsurface-0", api)
    {
        public override bool TakeLocked => be.craftingParams != null;
        public override bool PutLocked => be.craftingParams != null;
    }
}

public class CraftingParams
{
    public IPlayer Player;
    public bool Bulk;
    public bool OtherPlayer;
    public float RecipeCraftingTimeModifier;
    public EnumCraftingAnimation Animation;
    public ICraftingResult Recipe;
    public float NextCraftingTime;
    public BlockFacing? Facing;
    public int Amount;
}

// We don't use the DummySlot that comes with VanillaAPI, because the game assumes it is only used by handbook.
internal class DummySlot : ItemSlot
{
    public DummySlot(ItemStack? stack) : base(null)
    {
        Itemstack = stack;
        MarkDirty();
    }
}