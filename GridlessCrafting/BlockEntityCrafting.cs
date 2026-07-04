using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace RKN.GridlessCrafting;

public class BlockEntityCrafting : BlockEntity
{
    private InventoryGeneric inventory;

    private GridRecipe? selectedRecipe;
    private List<GridRecipe>? validRecipes;
    private IPlayer? craftingPlayer;
    private EnumCraftingAnimation? craftingAnimation;
    private float timeoutTimer;
    private long tickListenerId;
    private float secondsLastCraft;
    private int recipeId = -1;
    private bool recipeIdIsBlock = false;

    public BlockEntityCrafting()
    {
        inventory = new InventoryGeneric(9, "crafting", "0", null, null);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        inventory.LateInitialize("crafting-" + Pos.ToString(), api);
        if (recipeId > 0 && RecipeCatalog.IsInitialized())
        {
            selectedRecipe = RecipeCatalog.GetRecipeById(recipeId, recipeIdIsBlock);
        }
        if (Api.Side == EnumAppSide.Server)
        {
            tickListenerId = RegisterGameTickListener(OnTimeoutTick, 1000);
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        base.GetBlockInfo(forPlayer, sb);
        foreach (ItemSlot itemSlot in inventory)
        {
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
            sb.AppendLine();
        }
        sb.AppendLine();
        if (selectedRecipe != null)
        {
            sb.AppendLine("Recipe: " + selectedRecipe.Output.ResolvedItemStack.GetName());
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        ITreeAttribute treeAttribute = tree.GetTreeAttribute("inventory");
        if (treeAttribute != null)
        {
            inventory.FromTreeAttributes(treeAttribute);
        }
        timeoutTimer = tree.GetFloat("timeoutTimer");
        recipeId = tree.GetInt("selectedRecipe");
        recipeIdIsBlock = tree.GetBool("selectedRecipeIsBlockId");
        if (RecipeCatalog.IsInitialized())
        {
            if (recipeId == -1)
            {
                selectedRecipe = null;
            } else
            {
                selectedRecipe = RecipeCatalog.GetRecipeById(recipeId, recipeIdIsBlock);
            }
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        TreeAttribute treeAttribute = new();
        inventory.ToTreeAttributes(treeAttribute);
        tree["inventory"] = treeAttribute;
        tree.SetFloat("timeoutTimer", timeoutTimer);
        tree.SetBool("selectedRecipeIsBlockId", selectedRecipe?.Output?.ResolvedItemStack?.Block != null);
        tree.SetInt("selectedRecipe", selectedRecipe?.Output?.ResolvedItemStack?.Item?.Id ?? selectedRecipe?.Output?.ResolvedItemStack?.Block?.Id ?? -1);
    }

    public override void OnBlockBroken(IPlayer? byPlayer = null)
    {
        base.OnBlockBroken(byPlayer);
        if (Api != null && Api.Side == EnumAppSide.Server)
        {
            inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5), 0);
        }
    }

    public bool IsCrafting(IPlayer byPlayer)
    {
        return craftingPlayer == byPlayer;
    }

    public PlayerAnimationRequest? StartCrafting(IWorldAccessor world, IPlayer byPlayer, BlockCrafting blockCrafting)
    {
        timeoutTimer = 0;
        if (craftingPlayer != null || selectedRecipe == null)
        {
            return null;
        }
        (List<ItemSlot>? items, ItemSlot? primaryTool, ItemSlot? offhandTool) = GetCraftingItems(byPlayer);
        if (items == null)
        {
            return null;
        }
        craftingPlayer = byPlayer;
        craftingAnimation = GetCraftingAnimation(selectedRecipe, primaryTool, offhandTool);
        Api.Logger.Debug("[gridlesscrafting] Crafting {0} by {1}!", [selectedRecipe.Name, craftingPlayer.PlayerName]);
        if (world.Api.Side == EnumAppSide.Server)
        {
            MarkDirty();
        }
        return new PlayerAnimationRequest((EnumCraftingAnimation)craftingAnimation, EnumAnimationAction.START);;
    }

    public PlayerAnimationRequest? OnCraftingStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (Api.Side != EnumAppSide.Server)
        {
            return null;
        }
        timeoutTimer = 0;
        if (secondsUsed > (secondsLastCraft + 1) && IsCrafting(byPlayer))
        {
            CreateOutput(world);
            
            // Continue crafting if possible
            (List<ItemSlot>? items, ItemSlot? primaryTool, ItemSlot? offhandTool) = GetCraftingItems(craftingPlayer);
            if (items == null || items.Count == 0)
            {
                Api.World.BlockAccessor.BreakBlock(Pos, byPlayer);
            }
            if (items == null || !RecipeCatalog.MatchesRecipe(items, primaryTool, offhandTool, selectedRecipe, false))
            {
                EnumCraftingAnimation enumCraftingAnimation = GetCraftingAnimation();
                ResetState();
                selectedRecipe = null;
                (Api as ICoreServerAPI).Network.GetChannel("rkngridlesscrafting").SendPacket(new CraftingStoppedMessage() {animation = enumCraftingAnimation}, [(byPlayer as IServerPlayer)]);
                return new PlayerAnimationRequest(enumCraftingAnimation, EnumAnimationAction.STOP);
            }
            secondsLastCraft = secondsUsed;
        }
        return null;
    }

    public PlayerAnimationRequest? CancelCrafting(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        timeoutTimer = 0;
        if (craftingPlayer?.ClientId != byPlayer.ClientId)
        {
            return null;
        }
        Api.Logger.Debug("[gridlesscrafting] Cancelled crafting by {0}!", [craftingPlayer.PlayerName]);
        EnumCraftingAnimation anim = GetCraftingAnimation();
        ResetState();
        return new PlayerAnimationRequest(anim, EnumAnimationAction.STOP);
    }

    private EnumCraftingAnimation GetCraftingAnimation()
    {
        return (EnumCraftingAnimation)(craftingAnimation == null ? EnumCraftingAnimation.HandsMixing : craftingAnimation);
    }

    public bool TryPutIngredient(ItemSlot slot, IPlayer byPlayer)
    {
        timeoutTimer = 0;
        if (Api.Side != EnumAppSide.Server)
        {
            return false;
        }
        if (slot.Itemstack?.Item?.Tool != null)
        {
            return false;
        }
        foreach (ItemSlot invSlot in inventory)
        {
            if (invSlot.CanTakeFrom(slot))
            {
                int quantity = 1;
                if (byPlayer.Entity.Controls.CtrlKey)
                {
                    quantity = slot.StackSize;
                }
                if (slot.TryPutInto(Api.World, invSlot, quantity) < 1)
                {
                    return false;
                }
                slot.MarkDirty();

                (List<ItemSlot>? items, ItemSlot? _, ItemSlot? _) = GetCraftingItems(byPlayer);
                List<GridRecipe> recipes = RecipeCatalog.GetValidRecipesWithoutTools(items);
                validRecipes = recipes;
                if (recipes.Count > 0)
                {
                    selectedRecipe = validRecipes[0];
                }

                MarkDirty(true, null);
                return true;
            }
        }
        return false;
    }

    public void OnTimeoutTick(float dt)
    {
        timeoutTimer += dt;
        if(timeoutTimer >= 120)
        {
            Api.World.BlockAccessor.BreakBlock(Pos, null);
        }
    }

    private void ConsumeRecipe(GridRecipe recipe, List<ItemSlot> items, ItemSlot? primaryTool, ItemSlot? offhandTool, IWorldAccessor world)
    {
        foreach (CraftingRecipeIngredient? ingredient in recipe.ResolvedIngredients)
        {
            if (ingredient == null)
            {
                continue;
            }
            if (primaryTool != null && ingredient.ToolDurabilityCost > 0 && ingredient.SatisfiesAsIngredient(primaryTool.Itemstack, true))
            {
                primaryTool.Itemstack.Collectible.DamageItem(world, craftingPlayer.Entity, primaryTool, ingredient.ToolDurabilityCost, ingredient.Break);
                primaryTool.MarkDirty();
                continue;
            }
            else if (offhandTool != null && ingredient.ToolDurabilityCost > 0 && ingredient.SatisfiesAsIngredient(offhandTool.Itemstack, true))
            {
                offhandTool.Itemstack.Collectible.DamageItem(world, craftingPlayer.Entity, offhandTool, ingredient.ToolDurabilityCost, ingredient.Break);
                offhandTool.MarkDirty();
                continue;
            }
            foreach (ItemSlot stack in items)
            {
                if (stack.StackSize > 0 && ingredient.SatisfiesAsIngredient(stack.Itemstack, true))
                {
                    stack.TakeOut(ingredient.Quantity);
                    stack.MarkDirty();
                    goto CONTINUE;
                }
            }
            return;
            CONTINUE:;
        }
    }

    private (List<ItemSlot>? items, ItemSlot? primaryTool, ItemSlot? offhandTool) GetCraftingItems(IPlayer byPlayer)
    {
        List<ItemSlot> items = inventory.Where(s => s != null && s.StackSize > 0).ToList();
        if (items.Count == 0)
        {
            return (null, null, null);
        }
        IPlayerInventoryManager inventoryManager = byPlayer.InventoryManager;
        ItemSlot? primaryTool = inventoryManager.ActiveTool != null ? inventoryManager.ActiveHotbarSlot : null;
        ItemSlot? offhandTool = inventoryManager.OffhandTool != null ? inventoryManager.OffhandHotbarSlot : null;
        return (items, primaryTool, offhandTool);
    }

    private void CreateOutput(IWorldAccessor world)
    {
        if (craftingPlayer == null || selectedRecipe == null)
        {
            return;
        }
        (List<ItemSlot>? items, ItemSlot? primaryTool, ItemSlot? offhandTool) = GetCraftingItems(craftingPlayer);
        if (items == null || !RecipeCatalog.MatchesRecipe(items, primaryTool, offhandTool, selectedRecipe, false))
        {
            return;
        }
        Api.Logger.Debug("[gridlesscrafting] Crafted {0} by {1}!", [selectedRecipe.Name, craftingPlayer.PlayerName]);
        Api.World.SpawnItemEntity(selectedRecipe.Output.ResolvedItemStack.Clone(), Pos); // TODO: call OnCreatedByCrafting
        ConsumeRecipe(selectedRecipe, items, primaryTool, offhandTool, world);
    }

    private static EnumCraftingAnimation GetCraftingAnimation(GridRecipe recipe, ItemSlot? primaryTool, ItemSlot? offhandTool)
    {
        if (primaryTool == null)
        {
            if (recipe.Output?.ResolvedItemStack?.Item?.Tool != null) {
                return EnumCraftingAnimation.HandsTool;
            }
            return EnumCraftingAnimation.HandsMixing;
        }
        EnumTool? primary = primaryTool?.Itemstack?.Item?.Tool;
        EnumTool? offhand = offhandTool?.Itemstack?.Item?.Tool;
        return primary switch
        {
            EnumTool.Knife => EnumCraftingAnimation.Knife,
            EnumTool.Axe => offhand == EnumTool.Hammer ? EnumCraftingAnimation.AxeHammer : EnumCraftingAnimation.Axe,
            EnumTool.Hammer => EnumCraftingAnimation.Hammer,
            EnumTool.Shears => EnumCraftingAnimation.Shears,
            EnumTool.Saw => EnumCraftingAnimation.Saw,
            EnumTool.Chisel => offhand == EnumTool.Hammer ? EnumCraftingAnimation.ChiselHammer : EnumCraftingAnimation.Chisel,
            EnumTool.Club => EnumCraftingAnimation.Club,
            _ => EnumCraftingAnimation.HandsMixing
        };

    }

    private void ResetState()
    {
        craftingPlayer = null;
        craftingAnimation = null;
        secondsLastCraft = 0;
        MarkDirty();
    }
}