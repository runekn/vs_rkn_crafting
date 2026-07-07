using ProtoBuf;
using Vintagestory.API.MathTools;

namespace RKN.Crafting.Network;

[ProtoContract]
public class CreateCraftingBlockMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
}