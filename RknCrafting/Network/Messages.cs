using ProtoBuf;
using RKN.Crafting.Animation;
using RknCrafting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RKN.Crafting.Network;

[ProtoContract]
public class CreateCraftingBlockMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
    [ProtoMember(2)]
    public bool asPlayer = true;
}

[ProtoContract]
public class CraftingStoppedMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
    [ProtoMember(2)]
    public required EnumCraftingAnimation animation;
}

[ProtoContract]
public class SelectNextRecipeMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
}

[ProtoContract]
public class InventoryChangedMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
}

[ProtoContract]
public class ConfigMessage
{
    [ProtoMember(1)]
    public required RknCraftingConfig Config;
}

[ProtoContract]
public class ClientStartedCraftingMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
    [ProtoMember(2)]
    public required bool Bulk;
    [ProtoMember(3)]
    public required float RecipeCraftingTimeModifier;
    [ProtoMember(4)]
    public required EnumCraftingAnimation Animation;
    [ProtoMember(5)]
    public float NextCraftingTime;
    [ProtoMember(6)]
    public int Recipe;
    [ProtoMember(7)]
    public int Facing;
}

[ProtoContract]
public class PutToolIngredientMessage
{
    [ProtoMember(1)]
    public required BlockSelection BlockSelection;
}