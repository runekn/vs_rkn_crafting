using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RKN.Crafting.Animation;

public class CraftingAnimator
{
    public AnimationConfig Config;
    private ICoreAPI api;

    public CraftingAnimator(ICoreAPI api)
    {
        this.api = api;
        Config = api.Assets.Get("rkncrafting:config/rkncrafting_animations.json").ToObject<AnimationConfig>();
        ITagRegistry<TagSet> tagReg = api.CollectibleTagRegistry;
        foreach (AnimationConfigTagMatch tagMatch in Config.TagMatch)
        {
            if (tagReg.TryRegisterAndCreateTagSet(out TagSet tagSet, tagMatch.PrimaryTags) == TagRegistryError.None)
            {
                tagMatch.PrimaryTagSet = tagSet;
            }
            if (tagMatch.OffhandTags != null && tagReg.TryRegisterAndCreateTagSet(out TagSet tagSet2, tagMatch.OffhandTags) == TagRegistryError.None)
            {
                tagMatch.OffhandTagSet = tagSet2;
            }
        }
    }

    public string StartCrafting(IPlayer byPlayer, int recipe, ItemSlot? primaryTool, ItemSlot? offhandTool)
    {
        string animation = GetCraftingAnimation(recipe, primaryTool, offhandTool);
        StartCrafting(byPlayer, animation);
        return animation;
    }

    public void StartCrafting(IPlayer byPlayer, string animation)
    {
        if (!byPlayer.Entity.AnimManager.StartAnimation(animation))
        {
            api.RcLogger().Warning("Could not start animation: " + animation);
        }
    }

    public void StopCrafting(IPlayer byPlayer, string animation)
    {
        byPlayer.Entity.AnimManager.StopAnimation(animation);
    }

    private string GetCraftingAnimation(int recipe, ItemSlot? primaryTool, ItemSlot? offhandTool)
    {
        if (primaryTool == null)
        {
            if (api.RcRecipeService().GetRecipeById(recipe).RecipeWithTools.Output?.ResolvedItemStack?.Item?.Tool != null)
            {
                return Config.ToolCraftingAnimation;
            }
            return Config.DefaultAnimation;
        }

        TagSet? primary = primaryTool.Itemstack?.Item?.Tags;
        if (primary == null)
        {
            return Config.DefaultAnimation;
        }
        TagSet? offhand = offhandTool?.Itemstack?.Item?.Tags;
        foreach (AnimationConfigTagMatch tagMatch in Config.TagMatch)
        {
            if (tagMatch.PrimaryTagSet.Overlaps((TagSet) primary) &&
                (tagMatch.OffhandTagSet == null || (offhand != null && tagMatch.OffhandTagSet!.Value.Overlaps((TagSet)offhand))))
            {
                return tagMatch.AnimationCode;
            }
        }

        return Config.DefaultAnimation;
    }
}
