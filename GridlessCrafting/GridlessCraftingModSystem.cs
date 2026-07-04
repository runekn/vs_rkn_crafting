using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace RKN.GridlessCrafting;

public class GridlessCraftingModSystem : ModSystem
{
    private ICoreAPI api;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        this.api = api;
        api.RegisterBlockClass(Mod.Info.ModID + ".craftingblock", typeof(BlockCrafting));
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".craftingblock", typeof(BlockEntityCrafting));
        api.RegisterCollectibleBehaviorClass(Mod.Info.ModID + ".craftingstart", typeof(BehaviorStartCrafting));
        api.RegisterItemClass(Mod.Info.ModID + ".craftingwand", typeof(ItemCraftingWand));
        api.Logger.Debug("[gridlesscrafting] Hello world!");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Event.LevelFinalize += InitCatalog;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        InitCatalog();
    }

    public void InitCatalog()
    {
        RecipeCatalog.Initialize(api);
    }
}
