using HarmonyLib;
using RknCrafting.Entities;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace RknCrafting.Patches;

[HarmonyPatch(typeof(GuiHandbookItemStackPage), "PageCodeForStack")]
public class GuiHandbookItemStackPagePatch
{
    static bool Prefix(ref ItemStack stack)
    {
        if (stack.Item is ItemUnfinishedCraft)
        {
            ItemStack outputStack = ItemUnfinishedCraft.GetOutputStack(stack);
            if (outputStack != null)
            {
                stack = outputStack;
            }
        }
        return true;
    }
}