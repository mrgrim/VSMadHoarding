using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace MadHoarding;

public class BEMHCoolingCabinet : BECoolingCabinet
{
    protected override string ReferencedShape => ShapeReferences.CoolingCabinet;
    
    private enum SlotType {
        Segments = 8,
        LDoor = 9,
        RDoor = 10,
        IceDrawer = 11,
        ClosedCabinet = 12
    }

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var shift = byPlayer.Entity.Controls.ShiftKey;

        // Open/Close cabinet or drawer
        switch ((SlotType)blockSel.SelectionBoxIndex) {
            case SlotType.IceDrawer:
                if (shift) {
                    if (!DrawerOpen) ToggleDrawer(true, byPlayer);
                    else ToggleDrawer(false, byPlayer);
                    MarkDirty(true);
                    return true;
                }
                
                if (DrawerOpen && slot.Empty) return TryTakeIceOrSlush(byPlayer);
                break;

            case SlotType.ClosedCabinet:
                ToggleDoor(true, byPlayer);
                MarkDirty(true);
                return true;

            case SlotType.LDoor:
            case SlotType.RDoor:
                ToggleDoor(false, byPlayer);
                MarkDirty(true);
                return true;
        }

        if (DoorOpen && blockSel.SelectionBoxIndex <= (int)SlotType.Segments)
        {
            if (!(shift && slot.Empty) && TryUse(byPlayer, slot, blockSel)) return true;

            if (slot.Empty) return TryTake(byPlayer, blockSel);
        
            if (slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck)) {
                if (TryPut(byPlayer, slot, blockSel)) {
                    return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
                }
            }
        }

        if (!slot.Empty && DrawerOpen && slot.CanStoreInSlot(Constants.FSCoolingOnly)) {
            if (TryPutIce(byPlayer, slot, blockSel)) {
                return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
            }
        }

        // Removed the error because with the new interaction strategy the reason a placement didn't happen doesn't propagate enough 
        // information to produce a useful one. Instead, we'll rely on the improved interaction hints.
        return false;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        var shift = byPlayer.Entity.Controls.ShiftKey;
        var startIndex = blockSel.SelectionBoxIndex * ItemsPerSegment;
        
        if (startIndex >= inv.Count)
            return false;

        ItemStack? result;

        if (inv[startIndex].Itemstack?.Collectible is BaseFSContainer)
            result = shift && ((Block as BlockContainer)?.IsEmpty(inv[startIndex].Itemstack) ?? false)
                ? inv[startIndex].TakeOut(1) : null;
        else
            result = TryTakeFromSegment(byPlayer, startIndex);

        if (result is null) return false;

        if (byPlayer.InventoryManager.TryGiveItemstack(result))
            this.HandlePlacementEffects(result, byPlayer);
        if (result.StackSize > 0)
            Api.World.SpawnItemEntity(result, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        InitMesh();

        return true;
    }
}