using HarmonyLib;
using RKN.Crafting.Animation;
using RKN.Crafting.Entities;
using RKN.Crafting.Network;
using RKN.Crafting.Patches;
using RknCrafting;
using System;
using System.Linq;
using System.Reflection;
using RknCrafting.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace RKN.Crafting;

public class RknCraftingModSystem : ModSystem
{
#pragma warning disable CS8618
    private ICoreAPI api;
    private ICoreClientAPI capi => api as ICoreClientAPI;
    private Harmony harmony;
    private ActionConsumable<KeyCombination> oldToolModeHandler;

    public RknCraftingNetwork Network { get; internal set; }
    public RecipeService RecipeService { get; internal set; }
    public CraftingAnimator Animator { get; internal set; }
    public RknCraftingConfig ServerConfig { get; internal set; }
    public RknCraftingConfig LocalConfig { get; internal set; }
    public long BeginPauseInteractions { get; set; }
#pragma warning restore CS8618

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        this.api = api;
        TryLoadConfig();
        
        api.RegisterBlockClass(Mod.Info.ModID + ".craftingsurface", typeof(BlockCraftingSurface));
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".craftingsurface", typeof(BlockEntityCraftingSurface));
        api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".spawncraftingsurface", typeof(BlockBehaviorSpawnCraftingSurface));
        api.RegisterBlockBehaviorClass(Mod.Info.ModID + ".spawnchiselcraftingsurface", typeof(BlockBehaviorChiselSpawnCraftingSurface));
        api.RegisterItemClass(Mod.Info.ModID + ".unfinishedcraft", typeof(ItemUnfinishedCraft));
        api.RegisterBlockEntityBehaviorClass(Mod.Info.ModID + ".craftingsurfacemouseslotrecipient", typeof(BlockEntityBehaviorCraftingSurfaceInWorldInventory));

        Animator = new CraftingAnimator(api);
        
        ApplyHarmonyPatches();

        api.RcLogger().Debug("Hello world!");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Input.RegisterHotKey("rkncrafting.start", Lang.Get("rkncrafting:hotkey-crafting"), GlKeys.AltLeft);
        
        Network = new RknCraftingNetwork(api, Mod.Info.ModID);
        
        api.Input.InWorldAction += CheckPauseInteractions;
        api.Event.MouseUp += CheckResumeInteractions;

        api.Event.IsPlayerReady += AddRecipeSelectionHandler; // Add as late as possible since the vanilla handler is added at OnBlockTexturesLoaded

        if (!GuiDialogTransformEditor.extraTransforms.Any(c => c.AttributeName.Equals("craftingIngredientTransform")))
        {
            GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
            {
                AttributeName = "craftingIngredientTransform",
                Title = Lang.Get("rkncrafting:transform-craftingIngredientTransform")
            });
        }

        api.Gui.RegisterDialog(new HudMouseSlotInteract(api));
    }

    private bool AddRecipeSelectionHandler(ref EnumHandling handling)
    {
        if (api.Side != EnumAppSide.Client)
        {
            return true;
        }
        
        HotKey toolModeSelectHotkey = capi.Input.HotKeys["toolmodeselect"];
        oldToolModeHandler = toolModeSelectHotkey.Handler;
        toolModeSelectHotkey.Handler = CheckOpenRecipeSelection;
        return true;
    }

    private bool CheckOpenRecipeSelection(KeyCombination keys)
    {
        BlockSelection sel = capi.World.Player.Entity.BlockSelection;
        if (sel?.Block is BlockCraftingSurface)
        {
            BlockEntityCraftingSurface? entity = BlockCraftingSurface.GetBE(capi.World, sel.Position);
            entity?.OpenRecipeSelection();
            return true;
        }

        return oldToolModeHandler(keys);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        InitCatalog();
        Network = new RknCraftingNetwork(api, Mod.Info.ModID);
        api.Event.PlayerJoin += SendConfig;

        api.ChatCommands.Create("addcraft")
            .WithDescription("Spawn crafting surface with held item, without player replication. For testing.")
            .RequiresPrivilege(Privilege.controlserver)
            .RequiresPlayer()
            .HandleWith((args) =>
            {
                IPlayer byPlayer = args.Caller.Player;
                BlockSelection selection = byPlayer.CurrentBlockSelection;
                if (api.World.BlockAccessor.GetBlock(selection.Position) is BlockCraftingSurface block)
                {
                    BlockEntityCraftingSurface be = api.World.BlockAccessor.GetBlockEntity<BlockEntityCraftingSurface>(selection.Position);
                    ItemStackMoveOperation op = new(api.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, 1);
                    block.TryPutIngredient(api.World, be, byPlayer.InventoryManager.ActiveHotbarSlot, ref op, selection);
                }
                else
                {
                    if (!BlockCraftingSurface.TryPlace(api, null, selection.Position, byPlayer.InventoryManager.ActiveHotbarSlot))
                    {
                        return TextCommandResult.Error("Could not place crafting surface there");
                    }
                }
                return TextCommandResult.Success();
            });
    }

    public override void Dispose()
    {
        harmony.UnpatchAll(Mod.Info.ModID);
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        // Add behavior to all remaining blocks
        foreach (Block block in api.World.Blocks)
        {
            if (block.Code == null ||
                block.Id == 0 || 
                block.HasBehavior<BlockBehaviorSpawnCraftingSurface>())
            {
                continue;
            }
            block.BlockBehaviors = block.BlockBehaviors.Append(new BlockBehaviorSpawnCraftingSurface(block)).ToArray();
        }
    }

    private void ApplyHarmonyPatches()
    {
        harmony = new Harmony(Mod.Info.ModID);
        harmony.PatchAll();

        if (LocalConfig.DisableUICraftingGrid)
        {
            MethodInfo? original = typeof(GuiDialogInventory).DeclaredMethod("ComposeSurvivalInvDialog");
            MethodInfo? prefix = typeof(GuiDialogInventoryPatch).DeclaredMethod("ComposeSurvivalInvDialogPrefix");
            MethodInfo? original2 = typeof(GuiDialogInventory).DeclaredMethod("OnGuiClosed");
            MethodInfo? prefix2 = typeof(GuiDialogInventoryPatch).DeclaredMethod("OnGuiClosedPrefix");

            harmony.Patch(original, prefix: prefix);
            harmony.Patch(original2, prefix: prefix2);
        }

        if (LocalConfig.DisableInventoryGuiDialog)
        {
            MethodInfo? original = typeof(GuiDialogInventory).DeclaredMethod("TryOpen");
            MethodInfo? prefix = typeof(GuiDialogInventoryPatch).DeclaredMethod("TryOpenPrefix");
            harmony.Patch(original, prefix: prefix);
        }
    }

    private void TryLoadConfig()
    {
        string filename = Mod.Info.ModID + ".json";
        try
        {
            RknCraftingConfig config = api.LoadModConfig<RknCraftingConfig>(filename) ?? new RknCraftingConfig();
            api.StoreModConfig(config, filename);
            LocalConfig = config;
            if (api.Side == EnumAppSide.Server)
            {
                ServerConfig = config;                
            }
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Could not load config! Loading default settings instead.");
            Mod.Logger.Error(e);
            ServerConfig = new RknCraftingConfig();
            LocalConfig = new RknCraftingConfig();
        }
    }

    public void InitCatalog()
    {
        RecipeService = new RecipeService(api);
    }

    private void CheckResumeInteractions(MouseEvent e)
    {
        if (e.Button == EnumMouseButton.Right)
        {
            BeginPauseInteractions = 0;
        }
    }

    private void CheckPauseInteractions(EnumEntityAction action, bool on, ref EnumHandling handled)
    {
        if (action == EnumEntityAction.InWorldRightMouseDown && (Environment.TickCount - BeginPauseInteractions) < (LocalConfig.PauseInteractPostCraftSeconds * 1000))
        {
            handled = EnumHandling.PreventDefault;
        }
    }

    private void SendConfig(IServerPlayer byPlayer)
    {
        Network.TransferConfig(ServerConfig, byPlayer);
    }
}
