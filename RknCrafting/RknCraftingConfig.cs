using System;
using System.Collections.Generic;
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
    
    [ProtoMember(7)]
    public bool DisableUICraftingGrid = true;
    [ProtoIgnore]
    public string DisableUICraftingGrid_Comment = "Disables the inventory UI crafting grid.";
    
    [ProtoMember(8)]
    public bool DisableInventoryGuiDialog = false;
    [ProtoIgnore]
    public string DisableInventoryGuiDialog_Comment = "Disable inventory dialog (E) completely. This is only intended as compatibility with other mods that remove inventory grid.";
    
    [ProtoMember(9)]
    public bool EnableGridless = false;
    [ProtoIgnore]
    public string EnableGridless_Comment = "Crafting surfaces no longer have grid. Recipe matching will not care about what slot ingredients are in.";
    
    [ProtoMember(10)]
    public float PauseInteractPostCraftSeconds = 2.0f;
    [ProtoIgnore]
    public string PauseInteractPostCraftSeconds_Comment = "Block all right click interactions after crafting finished for this amount of seconds. This is to prevent unintentional actions right after crafting has ended when you are still holding right click. Releasing right click will also unblock interactions.";

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