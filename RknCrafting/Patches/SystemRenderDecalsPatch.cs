using HarmonyLib;
using RKN.Crafting.Entities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace RknCrafting.Patches;

[HarmonyPatch(typeof(SystemRenderDecals), "AddBlockBreakDecal")]
public class SystemRenderDecalsPatch
{
    static FieldInfo gameField = AccessTools.Field(typeof(SystemRenderDecals), "game");
    static FieldInfo apiField = AccessTools.Field(typeof(ClientMain), "api");

    static bool Prefix(BlockPos pos, int stage, ref object __result, SystemRenderDecals __instance)
    {
        ClientMain game = gameField.GetValue(__instance) as ClientMain;
        ICoreClientAPI api = apiField.GetValue(game) as ICoreClientAPI;
        if (api.World.BlockAccessor.GetBlock(pos) is BlockCraftingSurface)
        {
            __result = null;
            return false;
        }
        return true;
    }
}
