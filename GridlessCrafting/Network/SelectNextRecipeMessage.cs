using ProtoBuf;
using Vintagestory.API.MathTools;

namespace RKN.GridlessCrafting;

[ProtoContract]
public class SelectNextRecipeMessage
{
    [ProtoMember(1)]
    public required BlockPos Position;
}