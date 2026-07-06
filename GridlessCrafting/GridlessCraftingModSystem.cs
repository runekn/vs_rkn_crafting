using GridlessCrafting;
using HarmonyLib;
using RKN.GridlessCrafting.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace RKN.GridlessCrafting;

public class GridlessCraftingModSystem : ModSystem
{
    private ICoreAPI api;
    private Harmony harmony;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        this.api = api;
        api.RegisterBlockClass(Mod.Info.ModID + ".craftingsurface", typeof(BlockCrafting));
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".craftingsurface", typeof(BlockEntityCraftingSurface));
        api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".spawncraftingsurface", typeof(BlockBehaviorSpawnCraftingSurface));
        api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".spawncraftingsurface", typeof(CollectibleBehaviorSpawnCraftingSurface));
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();
        api.Logger.Debug("[gridlesscrafting] Hello world!");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Event.LevelFinalize += InitCatalog;
        api.Input.RegisterHotKey("rkngridlesscrafting.start", Lang.Get("hotkey-crafting"), GlKeys.AltLeft);
        GridlessCraftingNetwork.Initialize(api, Mod.Info.ModID);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        InitCatalog();
        GridlessCraftingNetwork.Initialize(api, Mod.Info.ModID);
    }

    public override void Dispose()
    {
        GridlessCraftingNetwork.Shutdown();
        RecipeCatalog.Shutdown();
        harmony.UnpatchAll(Mod.Info.ModID);
    }

    /*public override void AssetsFinalize(ICoreAPI api)
    {
        foreach (CollectibleObject collectible in api.World.Collectibles)
        {
            if (collectible.Code == null ||
                collectible.Id == 0 ||
                (collectible.ItemClass != EnumItemClass.Item && collectible.ItemClass != EnumItemClass.Block) || 
                (collectible is Item item && item.Tool != null) || 
                collectible.HasBehavior<CollectibleBehaviorSpawnCraftingSurface>())
            {
                continue;
            }
            CollectibleBehaviorSpawnCraftingSurface instance = new CollectibleBehaviorSpawnCraftingSurface(collectible);
            collectible.CollectibleBehaviors.Append(instance); // TODO: this isn't working...
        }
    }*/

    public void InitCatalog()
    {
        RecipeCatalog.Initialize(api);
    }
}
