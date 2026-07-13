using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace RKN.Crafting;

public class RecipeCatalog
{
    private ICoreAPI api;
    private List<GridRecipeWrapper> recipes;

    public RecipeCatalog(ICoreAPI api)
    {
        this.api = api;
        bool gridlesss = api.RCConfig().EnableGridless;
        recipes = new(api.World.GridRecipes.Count);
        for (int i = 0; i < api.World.GridRecipes.Count; i++)
        {
            GridRecipe recipe = api.World.GridRecipes[i];
            if (recipe.ResolvedIngredients == null)
            {
                continue;
            }
            // GridRecipe has RecipeId which the game doesn't seem to use itself. I'll steal it to connect FastSearchRecipeByIngredient to index.
            // I tried creating my own FastSearchRecipesByIngredient. But the vanilla map uses ingredients from before variants are resolved. So mine didn't work.
            recipe.RecipeId = i;
            GridRecipeWrapper wrapper = new(recipe, gridlesss, i);
            recipes.Add(wrapper);
            foreach (CraftingRecipeIngredient? ingredient in wrapper.RecipeWithoutTools.ResolvedIngredients)
            {
                if (ingredient == null)
                {
                    continue;
                }
            }
        }
    }

    public GridRecipeWrapper GetRecipeById(int id)
    {
        return recipes[id];
    }

    public List<int> GetValidRecipes(ItemSlot[] items, ItemSlot? primaryTool, ItemSlot? offhandTool, bool gridless, IPlayer byPlayer)
    {
        List<int> result = [];
        ItemStack? sample = items.First()?.Itemstack;
        if (sample == null)
        {
            return result;
        }
        long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        foreach (var pair in api.World.FastSearchRecipesByIngredient)
        {
            if (IngredientSatisfied(pair.Key, sample, null))
            {
                foreach (IRecipeBase recipe in pair.Value)
                {
                    if (recipe is not GridRecipe gridRecipe)
                    {
                        continue;
                    }
                    GridRecipeWrapper wrapper = recipes[gridRecipe.RecipeId];
                    if (MatchesRecipe(items, primaryTool, offhandTool, wrapper, gridless, byPlayer))
                    { 
                        result.Add(wrapper.Id);
                    }
                }
            }
        }
        long time = DateTimeOffset.Now.ToUnixTimeMilliseconds() - start;
        api.RCLogger().Debug("Scanning recipes took {0} ms", [time]);
        return result;
    }

    public bool MatchesRecipe(ItemSlot[] items, ItemSlot? primaryTool, ItemSlot? offhandTool, int recipeId, bool gridless, IPlayer byPlayer)
    {
        GridRecipeWrapper wrapper = recipes[recipeId];
        return MatchesRecipe(items, primaryTool, offhandTool, wrapper, gridless, byPlayer);
    }

    private bool MatchesRecipe(ItemSlot[] items, ItemSlot? primaryTool, ItemSlot? offhandTool, GridRecipeWrapper wrapper, bool gridless, IPlayer byPlayer)
    {
        if (!wrapper.RecipeWithoutTools.Enabled || wrapper.RecipeWithoutTools.ResolvedIngredients == null)
        {
            return false;
        }
        if (gridless)
        {
            // Use custom implementation, because vanilla shapeless matching does not handle scenarios:
                // - Multiple recipe slots of same ingredient fulfilled by one large input stack.
                // - Probably more...
            if (!MatchesRecipeGridless(items, primaryTool, offhandTool, wrapper.RecipeWithoutTools, byPlayer))
            {
                return false;
            }
        } else
        {
            if (!wrapper.RecipeWithoutTools.Matches(byPlayer, api.World, items, 3))
            {
                return false;
            }
        }
        foreach (CraftingRecipeIngredient ingredient in wrapper.ToolIngredients)
        {
            if (!IngredientSatisfied(ingredient, primaryTool?.Itemstack, wrapper.RecipeWithoutTools) && 
                !IngredientSatisfied(ingredient, offhandTool?.Itemstack, wrapper.RecipeWithoutTools))
            {
                return false;
            }
        }
        return true;
    }

    private bool MatchesRecipeGridless(ItemSlot[] items, ItemSlot? primaryTool, ItemSlot? offhandTool, GridRecipe recipe, IPlayer byPlayer)
    {
        if (!api.Event.TriggerMatchesRecipe(byPlayer, recipe, items))
        {
            return false;
        }
        List<ItemStack> clonedItems = items.Select(i => i?.Itemstack?.Clone()).Where(i => i != null).ToList();
        if (clonedItems.Count == 0)
        {
            return false;
        }
        MergeStacks(clonedItems);
        clonedItems = clonedItems.Where(s => s.StackSize > 0).ToList(); // TODO: I don't like creating list again
        ISet<ItemStack> unusedItems = clonedItems.ToHashSet();
        foreach (CraftingRecipeIngredient? ingredient in recipe.ResolvedIngredients)
        {
            if (ingredient == null)
            {
                continue;
            }
            if (!MatchesIngredientGridless(recipe, clonedItems, primaryTool, offhandTool, ingredient, unusedItems))
            {
                return false;
            }
        }
        if (unusedItems.Count > 0)
        {
            return false;
        }
        return true;
    }

    protected virtual void MergeStacks(List<ItemStack> stacks)
    {
        for (int i = 1; i < stacks.Count; i++)
        {
            ItemStack stack1 = stacks[i];
            for (int j = 0; j < i; j++)
            {
                ItemStack stack2 = stacks[j];
                if (stack2.Satisfies(stack1))
                {
                    stack2.StackSize += stack1.StackSize;
                    stack1.StackSize = 0;

                }
            }
        }
    }

    private bool MatchesIngredientGridless(GridRecipe recipe, IEnumerable<ItemStack> items, ItemSlot? primaryTool, ItemSlot? offhandTool, CraftingRecipeIngredient ingredient, ISet<ItemStack> unusedItems)
    {
        bool satisfied = false; // Instead of just return true on the first item that satisfies ingredient, we need to loop through all so that all satisfying stacks can be removed from unusedItems.
        foreach (ItemStack stack in items)
        {
            if (IngredientSatisfied(ingredient, stack, recipe))
            {
                unusedItems.Remove(stack);
                if (!satisfied)
                {
                    satisfied = true;
                    stack.StackSize -= ingredient.Quantity;
                }
            }
        }
        if (satisfied)
        {
            return true;
        }
        return false;
    }

    private bool IngredientSatisfied(IRecipeIngredientBase ingredient, ItemStack? stack, GridRecipe? recipe)
    {
        return stack != null && stack.StackSize > 0 && ingredient.SatisfiesAsIngredient(stack, true) && (recipe == null || stack.Collectible.MatchesForCrafting(stack, recipe, ingredient as IRecipeIngredient));
    }
}

public class GridRecipeWrapper
{
    public GridRecipe RecipeWithoutTools;
    public GridRecipe RecipeWithTools;
    public int Id;
    public List<CraftingRecipeIngredient> ToolIngredients = [];

    public GridRecipeWrapper(GridRecipe recipe, bool gridless, int id)
    {
        this.RecipeWithTools = recipe;
        this.RecipeWithoutTools = recipe.Clone();
        Id = id;
        if (gridless)
        {
            RecipeWithoutTools.Shapeless = true;
        }
        for (int i = 0; i < RecipeWithoutTools.ResolvedIngredients.Length; i++)
        {
            CraftingRecipeIngredient? ingredient = RecipeWithoutTools.ResolvedIngredients[i];
            if (ingredient != null && !ingredient.Consume)
            {
                RecipeWithoutTools.ResolvedIngredients[i] = null;
                ToolIngredients.Add(ingredient);
            }
        }
    }
}