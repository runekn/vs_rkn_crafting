using ProtoBuf;

namespace RKN.Crafting.Network;

[ProtoContract]
public class CraftingStoppedMessage
{
    [ProtoMember(1)]
    public required EnumCraftingAnimation animation;
}