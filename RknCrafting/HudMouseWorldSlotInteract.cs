using System.Linq;
using RKN.Crafting;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace RknCrafting;

public class HudMouseWorldSlotInteract : HudElement
{
    public override double DrawOrder => 1.01; // Little larger than HudDropItem

    public HudMouseWorldSlotInteract(ICoreClientAPI capi)
        : base(capi)
    {
        TryOpen();
    }

    public override bool TryClose() => false;

    public override void OnMouseDown(MouseEvent args)
    {
        if (args.Handled || capi.Input.MouseGrabbed)
            return;
        foreach (GuiDialog openedGui in capi.Gui.OpenedGuis)
        {
            if (openedGui.IsOpened() && openedGui is not HudMouseTools && openedGui is not HudIngameDiscovery && openedGui is not HudElementInteractionHelp)
            {
                foreach (GuiComposer guiComposer in openedGui.Composers.Values)
                {
                    if (guiComposer.Bounds.PointInside(args.X, args.Y))
                        return;
                }
            }
        }

        BlockSelection? blockSelection = capi.World.Player.CurrentBlockSelection;
        if (blockSelection == null)
        {
            return;
        }
        IBlockInWorldInventory? blockMouseSlotRecipient = capi.World.BlockAccessor.GetBlock(blockSelection.Position)?.GetInterface<IBlockInWorldInventory>(capi.World, blockSelection.Position);
        if (blockMouseSlotRecipient == null)
        {
            return;
        }
        ItemSlot mouseSlot = capi.World.Player.InventoryManager.MouseItemSlot;
        ItemSlot? slot = blockMouseSlotRecipient.GetItemSlot(blockSelection, mouseSlot, args, out int slotId);
        if (slot == null)
        {
            return;
        }

        bool shift = capi.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || capi.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
        bool ctrl = capi.Input.KeyboardKeyState[(int)GlKeys.ControlLeft];
        bool alt = capi.Input.KeyboardKeyState[(int)GlKeys.AltLeft];
        EnumModifierKey modifiers = shift ? EnumModifierKey.SHIFT : ctrl ? EnumModifierKey.CTRL : alt ?  EnumModifierKey.ALT : 0;
        ItemStackMoveOperation op = new(capi.World, args.Button, modifiers, EnumMergePriority.AutoMerge, 1)
        {
            ActingPlayer = capi.World.Player
        };
        object packet = slot.Inventory.ActivateSlot(slotId, mouseSlot, ref op);
        blockMouseSlotRecipient.OnModified(blockSelection, op.ActingPlayer);
        if (packet is Packet_Client packetClient)
        {
            // We're doing this through custom channel because the vanilla packet handler (ServerSystemInventory.HandleActivateInventorySlot) can't find arbitrary inventory by id in packet.
            // In custom channel I can get inventory by block selection.
            capi.RcNetwork().BlockMouseSlotInteraction(packetClient, blockSelection);
        }
    }

    public override bool ShouldReceiveKeyboardEvents() => true;

    public override bool Focusable => false;
}