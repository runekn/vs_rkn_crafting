using System.Collections.Generic;
using HarmonyLib;
using Vintagestory.API.Common;

namespace RKN.GridlessCrafting;

[HarmonyPatch(typeof(RecipeBase), "GenerateRecipesForTagOnlyIngredients")]
public class GridRecipePatch
{
    static bool Prefix(IWorldAccessor world, IRecipeBase recipe, ref IEnumerable<IRecipeBase> __result, IRecipeBase __instance)
    {
        __result = [recipe];
        return false;
    }
}