using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using ProtoBuf;

// ReSharper disable InconsistentNaming

namespace RknCrafting;

[ProtoContract]
public class RknCraftingConfig
{
    [ProtoMember(1)]
    public float BaseCraftingTimeSeconds = 1.0f;
    [ProtoIgnore]
    public string BaseCraftingTimeSeconds_Comment = "Base seconds to craft.";
    
    [ProtoMember(2)]
    public int AutoDeleteTimeSeconds = 120;
    [ProtoIgnore]
    public string AutoDeleteTimeSeconds_Comment = "How many seconds of inactivity it takes for crafting surface to self-delete.";
    
    [ProtoMember(3)]
    public float ConsecutiveCraftingTimeModifier = 0.95f;
    [ProtoIgnore]
    public string ConsecutiveCraftingTimeModifier_Comment = "Amount to decrease time to craft while continuing to hold right click.";
    
    [ProtoMember(4)]
    public float ConsecutiveCraftingTimeModifierMin = 0.5f;
    [ProtoIgnore]
    public string ConsecutiveCraftingTimeModifierMin_Comment = "The minimum amount that crafting time can be decreased during consecutive crafting.";
    
    [ProtoMember(5)]
    public bool EnableBulkCrafting = false;
    [ProtoIgnore]
    public string EnableBulkCrafting_Comment = "Allows for holding SHIFT while crafting to craft as many items as possible at once.";
    
    [ProtoMember(6)]
    public float BulkBaseCraftingTimeSeconds = 2.0f;
    [ProtoIgnore]
    public string BulkBaseCraftingTimeSeconds_Comment = "Base seconds to craft if using bulk crafting.";
    
    [ProtoIgnore]
    public bool DisableUICraftingGrid = true;
    [ProtoIgnore]
    public string DisableUICraftingGrid_Comment = "Disables the inventory UI crafting grid.";
    
    [ProtoIgnore]
    public bool DisableInventoryGuiDialog = false;
    [ProtoIgnore]
    public string DisableInventoryGuiDialog_Comment = "Disable inventory dialog (E) completely. This is only intended as compatibility with other mods that remove inventory grid.";
    
    [ProtoMember(7)]
    public bool EnableGridless = false;
    [ProtoIgnore]
    public string EnableGridless_Comment = "Crafting surfaces no longer have grid. Recipe matching will not care about what slot ingredients are in.";
    
    [ProtoIgnore]
    public float PauseInteractPostCraftSeconds = 2.0f;
    [ProtoIgnore]
    public string PauseInteractPostCraftSeconds_Comment = "Block all right click interactions after crafting finished for this amount of seconds. This is to prevent unintentional actions right after crafting has ended when you are still holding right click. Releasing right click will also unblock interactions.";
    
    [ProtoIgnore]
    public bool RenderAllSlots = false;
    [ProtoIgnore]
    public string RenderAllSlots_Comment = "Render selection boxes for all slots at all times. Turning this config off is cleaner and makes it easier to see which slot you are aiming at. Turning it on can make it easier to get an overview of the whole grid on large surfaces. This config has no effect in gridless mode.";

    [ProtoIgnore]
    public bool EnableFloatingCount = true;
    [ProtoIgnore]
    public string EnableFloatingCount_Comment = "Display an in-world slot count. Only relevant for grid mode.";
    
    [ProtoMember(8)]
    [DefaultValue(true)]
    public bool EnableExperimentalToolDetection = true; // TODO: WHY DO YOU NOT WORK!
    [ProtoIgnore]
    public string EnableExperimentalToolDetection_Comment = "Better support for mod tools. Optional for now until I'm sure it doesn't break something.";

    [ProtoMember(9)]
    [DefaultValue(true)]
    public bool EnableHoldConsumedTools = true;
    [ProtoIgnore]
    public string EnableHoldConsumedTools_Comment = "Tools that are consumed must still be held in hand (as opposed to added as ingredient).";

    [ProtoIgnore]
    public int DebugVerbosity = 1;
    [ProtoIgnore]
    public string DebugVerbosity_Comment = "How verbose debug log should be. 0 = no debug logs. 1 = basic debug. 2 = very verbose.";

    // If adding new [ProtoMember] that has default value which is different from the primitive type's default, you need to add [DefaultValue]
    
    public override string ToString()
    {
        JsonSerializer jsonSerializer = JsonSerializer.CreateDefault();
        jsonSerializer.ContractResolver = new UncommentContractResolver();
        return JObject.FromObject(this, jsonSerializer).ToString();
    }
}

public class UncommentContractResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        return base.CreateProperties(type, memberSerialization).Where(p => p.PropertyName == null || !p.PropertyName.EndsWith("_Comment")).ToList();
    }
}