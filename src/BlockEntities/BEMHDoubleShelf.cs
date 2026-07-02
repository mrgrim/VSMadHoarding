using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MadHoarding;

public class BEMHDoubleShelf : BEDoubleShelf
{
    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        //GetSlotAt(blockSel);
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var shift = byPlayer.Entity.Controls.ShiftKey;

        if (!(shift && slot.Empty) && TryUse(byPlayer, slot, blockSel)) return true;

        if (slot.Empty) return TryTake(byPlayer, blockSel);

        if (slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck))
        {
            if (TryPut(byPlayer, slot, blockSel))
            {
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

    // Toying with the idea of detecting columns inside segments...
    protected Vec3f rotatedOffset(Vec3f offset)
    {
        var matrix = new Matrixf();

        var radY = block.Variant["side"] switch
        {
            "west"  => -90f  * ((float)Math.PI / 180f),
            "south" => -180f * ((float)Math.PI / 180f),
            "east"  => -270f * ((float)Math.PI / 180f),
            _       => 0f
        };
        
        matrix.Translate(0.5f, 0.5f, 0.5f).RotateY(radY).Translate(-0.5f, -0.5f, -0.5f);
        return matrix.TransformVector(new Vec4f(offset.X, offset.Y, offset.Z, 1f)).XYZ;
    }
    
    public void GetSlotAt(BlockSelection bs)
    {
        var vec3f = rotatedOffset(bs.HitPosition.ToVec3f());
        var segmentPos = block.SelectionBoxes[bs.SelectionBoxIndex];
        
        if (Api is ICoreClientAPI api)
            api.TriggerIngameDiscovery(this, "DoubleShelfGetSlotAt", $"({vec3f})/{segmentPos}/{bs.SelectionBoxIndex}");
    }
}