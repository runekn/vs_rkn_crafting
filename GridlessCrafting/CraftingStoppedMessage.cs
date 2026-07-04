using ProtoBuf;

namespace RKN.GridlessCrafting;

[ProtoContract]
public class CraftingStoppedMessage
{
    [ProtoMember(1)]
    public required EnumCraftingAnimation animation;
}