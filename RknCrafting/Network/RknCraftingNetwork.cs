using RKN.Crafting.Animation;
using RKN.Crafting.Entities;
using RknCrafting;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace RKN.Crafting.Network;

public class RknCraftingNetwork
{
    private ICoreAPI api;
    private INetworkChannel channel;
    private INetworkChannel channelUdp;

#pragma warning disable CS8603
    private IServerNetworkChannel ServerChannel { get { return channel as IServerNetworkChannel; } }
    private IClientNetworkChannel ClientChannel { get { return channel as IClientNetworkChannel; } }
    private IServerNetworkChannel ServerChannelUdp { get { return channelUdp as IServerNetworkChannel; } }
    private IClientNetworkChannel ClientChannelUdp { get { return channelUdp as IClientNetworkChannel; } }
    private ICoreClientAPI ClientApi { get { return api as ICoreClientAPI; } }
    private ICoreServerAPI ServerApi { get { return api as ICoreServerAPI; } }
#pragma warning restore CS8603

    public RknCraftingNetwork(ICoreClientAPI api, string modId)
    {
        this.api = api;
        channel = api.Network.RegisterChannel(modId);
        channelUdp = api.Network.RegisterUdpChannel(modId + "-udp");

        ClientChannel.RegisterMessageType<CreateCraftingBlockMessage>();
        ClientChannel.RegisterMessageType<CraftingStoppedMessage>();
        ClientChannel.RegisterMessageType<SelectNextRecipeMessage>();
        ClientChannel.RegisterMessageType<InventoryChanged>();
        ClientChannel.RegisterMessageType<ConfigMessage>();
        ClientChannel.SetMessageHandler<CraftingStoppedMessage>(OnCraftingStoppedMessage);
        ClientChannel.SetMessageHandler<InventoryChanged>(OnInventoryChangedMessage);
        ClientChannel.SetMessageHandler<ConfigMessage>(OnConfigMessage);

        ClientChannelUdp.RegisterMessageType<InventoryChanged>();
        ClientChannelUdp.SetMessageHandler<InventoryChanged>(OnInventoryChangedMessage);
    }

    public RknCraftingNetwork(ICoreServerAPI api, string modId)
    {
        this.api = api;
        channel = api.Network.RegisterChannel(modId);
        channelUdp = api.Network.RegisterUdpChannel(modId + "-udp");

        ServerChannel.RegisterMessageType<CreateCraftingBlockMessage>();
        ServerChannel.RegisterMessageType<CraftingStoppedMessage>();
        ServerChannel.RegisterMessageType<SelectNextRecipeMessage>();
        ServerChannel.RegisterMessageType<InventoryChanged>();
        ServerChannel.RegisterMessageType<ConfigMessage>();
        ServerChannel.SetMessageHandler<CreateCraftingBlockMessage>(OnCreateCraftingBlockMessage);
        ServerChannel.SetMessageHandler<SelectNextRecipeMessage>(OnSelectNextRecipeMessage);

        ServerChannelUdp.RegisterMessageType<InventoryChanged>();
    }

    public void SelectNextRecipe(BlockPos pos)
    {
        ClientChannel.SendPacket(new SelectNextRecipeMessage() { Position = pos });
    }

    protected void OnSelectNextRecipeMessage(IServerPlayer fromPlayer, SelectNextRecipeMessage message)
    {
        api.World.BlockAccessor.GetBlockEntity<BlockEntityCraftingSurface>(message.Position).SelectNextRecipe();
    }

    public void SpawnCraftingSurface(BlockPos pos)
    {
        ClientChannel.SendPacket(new CreateCraftingBlockMessage() { Position = pos });
    }

    protected void OnCreateCraftingBlockMessage(IPlayer fromPlayer, CreateCraftingBlockMessage message)
    {
        BlockCraftingSurface.TryPlace(api, fromPlayer, message.Position, fromPlayer.InventoryManager.ActiveHotbarSlot);
    }

    public void StopCrafting(IPlayer craftingPlayer, EnumCraftingAnimation enumCraftingAnimation)
    {
        ServerChannel.SendPacket(new CraftingStoppedMessage() { animation = enumCraftingAnimation }, [(craftingPlayer as IServerPlayer)]);
    }

    protected void OnCraftingStoppedMessage(CraftingStoppedMessage message)
    {
        api.RCLogger().Debug("Received stop crafting message!");
        IPlayer player = ClientApi.World.Player;
        api.RCAnimator().StopCrafting(player, message.animation);
    }

    public void RecipeConsumed(BlockPos pos)
    {
        api.RCLogger().Debug("Broadcasting recipe consumed message!");
        ServerChannel.BroadcastPacket(new InventoryChanged() { Position = pos });
    }

    private void OnInventoryChangedMessage(InventoryChanged packet)
    {
        api.RCLogger().Debug("Received recipe consumed message!");
        BlockEntityCraftingSurface.OnInventoryUpdated(ClientApi, packet.Position);
    }

    public void TransferConfig(RknCraftingConfig config, IServerPlayer player)
    {
        api.RCLogger().Debug("Sending config to player {0}: {1}", [player.PlayerName, config]);
        ServerChannel.SendPacket(new ConfigMessage() { Config = config }, [player]);
    }

    private void OnConfigMessage(ConfigMessage message)
    {
        api.RCLogger().Debug("Received config from server: {0}", [message.Config]);
        api.RCSetConfig(message.Config);
    }

    public void IngredientAdded(BlockPos pos, IPlayer byPlayer)
    {
        ServerChannelUdp.BroadcastPacket(new InventoryChanged() { Position = pos }, [byPlayer as IServerPlayer]);
    }
}
