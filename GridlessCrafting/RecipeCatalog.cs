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
        //catalog = api.World.GridRecipes.Select(r => { r = r.Clone(); r.Shapeless = true; return r; }).ToList();
        catalog = [.. api.World.GridRecipes];
        RecipeCatalog.api = api;
    }

    public static bool IsInitialized()
    {
        return catalog != null;
    }

    public static GridRecipe? GetRecipeById(int id, bool idIsBlock)
    {
        if (idIsBlock)
        {
            return catalog.Find(r => r.Output?.ResolvedItemStack?.Block?.Id == id);
        }
        return catalog.Find(r => r.Output?.ResolvedItemStack?.Item?.Id == id);
    }

    public static List<GridRecipe> GetValidRecipesWithoutTools(List<ItemSlot> items)
    {
        List<GridRecipe> result = [];
        foreach (GridRecipe recipe in catalog)
        {
            if (MatchesRecipe(items, null, null, recipe, true))
            {
                result.Add(recipe);
            }
        }
        return result;
    }

    public static bool MatchesRecipe(List<ItemSlot> items, ItemSlot? primaryTool, ItemSlot? offhandTool, GridRecipe recipe, bool ignoreTools)
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
            if (!MatchesIngredient(clonedItems, primaryTool, offhandTool, ingredient, ignoreTools))
            {
                return false;
            }
        }
        return true;
    }

    private static bool MatchesIngredient(IEnumerable<ItemStack> items, ItemSlot? primaryTool, ItemSlot? offhandTool, CraftingRecipeIngredient ingredient, bool ignoreTools)
    {
        if (!ingredient.Consume) // TODO: Why does ingredient.IsTool not work? Try replacing with ingredient.Consume
        {
            if (ignoreTools)
            {
                return true;;
            }
            if (primaryTool != null && ingredient.SatisfiesAsIngredient(primaryTool.Itemstack, true))
            {
                return true;
            }
            else if (offhandTool != null && ingredient.SatisfiesAsIngredient(offhandTool.Itemstack, true))
            {
                return true;
            }
            return false;
        }
        else
        {
            foreach (ItemStack stack in items)
            {
                if (stack.StackSize > 0 && ingredient.SatisfiesAsIngredient(stack, true))
                {
                    stack.StackSize -= ingredient.Quantity;
                    return true;
                }
            }
            return false;
        }
    }
}