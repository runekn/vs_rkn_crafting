using System.Collections.Generic;
using System.Text;
using RKN.Crafting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RknCrafting.Entities;

public class ItemUnfinishedCraft : Item
{
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        AssetLocation?[]? assets = GetUsedTools(inSlot.Itemstack!);
        if (assets == null)
        {
            dsc.Append("ERROR: Could not get used tools!");
            return;
        }
        dsc.Append("Used tools: ");
        bool first = true;
        foreach (AssetLocation? asset in assets)
        {
            if (asset == null)
            {
                continue;
            }

            string name = world.GetItem(asset).GetHeldItemName(null); // Hope this doesn't crash from NPE...
            if (!first)
            {
                dsc.Append(", ");
            }
            first = false;
            dsc.Append(name);
        }
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        ItemStack outputStack = itemStack.Attributes.GetItemstack("output");
        if (outputStack == null)
        {
            return "ERROR: Unknown output";
        }
        outputStack.ResolveBlockOrItem(api.World);
        return "Unfinished " + outputStack.GetName();
    }

    public static void PopulateAttributes(ItemStack stack, ItemStack output, GridRecipeWrapper recipe, AssetLocation?[] outputUsedTools)
    {
        stack.Attributes.SetItemstack("output", output);
        stack.Attributes.SetInt("recipe", recipe.Id);
        ITreeAttribute usedToolsTree = new TreeAttribute();
        for (int i = 0; i < outputUsedTools.Length; i++)
        {
            AssetLocation? usedTool = outputUsedTools[i];
            if (usedTool == null)
            {
                continue;
            }
            usedToolsTree.SetString(i.ToString(), usedTool.ToString());
        }
        stack.Attributes["usedTools"] = usedToolsTree;
    }

    public static AssetLocation?[]? GetUsedTools(ItemStack stack, GridRecipeWrapper? recipe = null)
    {
        ITreeAttribute? tree = stack.Attributes.GetTreeAttribute("usedTools");
        if (tree == null)
        {
            return null;
        }
        AssetLocation?[] r = new AssetLocation?[recipe?.ToolIngredients.Count ?? tree.Count];
        int i = 0;
        foreach (KeyValuePair<string, IAttribute> pair in tree)
        {
            int index = int.Parse(pair.Key);
            AssetLocation asset = new((pair.Value as StringAttribute).value);
            index = recipe == null ? i++ : index;
            if (index >= r.Length)
            {
                return null; // This may happen due to recipe id no longer pointing to the correct recipe.
            }
            r[index] = asset;
        }

        return r;
    }

    public static int GetOutputRecipe(ItemStack stack)
    {
        return stack.Attributes.GetInt("recipe", RecipeService.RecipeIdNone);
    }

    public static ItemStack? GetOutputStack(ItemStack stack)
    {
        return stack.Attributes.GetItemstack("output");
    }
}