using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Datastructures;

namespace RKN.Crafting.Animation;

public class AnimationConfig
{
    public string DefaultAnimation;
    public string ToolCraftingAnimation;
    public List<AnimationConfigTagMatch> TagMatch;
}

public class AnimationConfigTagMatch
{
    public string AnimationCode;
    public string[] PrimaryTags;
    public string[]? OffhandTags;
    [JsonIgnore]
    public TagSet PrimaryTagSet;
    [JsonIgnore]
    public TagSet? OffhandTagSet;
}