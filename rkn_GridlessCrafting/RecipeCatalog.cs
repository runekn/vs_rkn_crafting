using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace RKN.GridlessCrafting;

public class RecipeCatalog
{
    private static List<GridRecipe> catalog;
    private static ICoreAPI api;

    public static void Initialize(ICoreAPI api)
    {
        catalog = [.. api.World.GridRecipes];
        RecipeCatalog.api = api;
    }

    public static List<GridRecipe> GetValidRecipes(List<ItemSlot> items, ItemSlot? primaryTool, ItemSlot? offhandTool)
    {
        List<GridRecipe> result = [];
        foreach (GridRecipe recipe in catalog)
        {
            if (MatchesRecipe(items, primaryTool, offhandTool, recipe))
            {
                result.Add(recipe);
            }
        }
        return result;
    }

    public static bool MatchesRecipe(List<ItemSlot> items, ItemSlot? primaryTool, ItemSlot? offhandTool, GridRecipe recipe)
    {
        if (!recipe.Enabled || recipe.ResolvedIngredients == null)
        {
            return false;
        }
        IEnumerable<ItemStack> clonedItems = items.Select(i => i.Itemstack.Clone()).AsEnumerable();
        foreach (CraftingRecipeIngredient? ingredient in recipe.ResolvedIngredients)
        {
            if (ingredient == null)
            {
                continue;
            }
            if (primaryTool != null && ingredient.SatisfiesAsIngredient(primaryTool.Itemstack, true))
            {
                /*if (!ingredient.IsTool)
                {
                    api.Logger.Debug("Lying piece of shit");
                }*/
                continue;
            }
            else if (offhandTool != null && ingredient.SatisfiesAsIngredient(offhandTool.Itemstack, true))
            {
                /*if (!ingredient.IsTool)
                {
                    api.Logger.Debug("Lying piece of shit");
                }*/
                continue;
            }
            foreach (ItemStack stack in clonedItems)
            {
                if (stack.StackSize > 0 && ingredient.SatisfiesAsIngredient(stack, true))
                {
                    stack.StackSize -= ingredient.Quantity;
                    goto CONTINUE;
                }
            }
            return false;
            CONTINUE:;
            /*if (ingredient.IsTool != null) // TODO: Why does ingredient.IsTool not work? Try replacing with ingredient.Consume
            {
                if (primaryTool != null && ingredient.SatisfiesAsIngredient(primaryTool.Itemstack, true))
                {
                    continue;
                }
                else if (offhandTool != null && ingredient.SatisfiesAsIngredient(offhandTool.Itemstack, true))
                {
                    continue;
                }
                return false;
            }
            else
            {
                foreach (ItemStack stack in clonedItems)
                {
                    if (stack.StackSize > 0 && ingredient.SatisfiesAsIngredient(stack, true))
                    {
                        stack.StackSize -= ingredient.Quantity;
                        goto CONTINUE;
                    }
                }
                return false;
            }
            CONTINUE:;*/
        }
        return true;
    }
}