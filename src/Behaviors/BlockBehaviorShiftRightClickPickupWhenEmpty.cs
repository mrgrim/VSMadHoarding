using FoodShelves;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace MadHoarding;

public class BlockBehaviorShiftRightClickPickupWhenEmpty(Block block) : BlockBehaviorShiftRightClickPickup(block)
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityContainer be) return false;
        return be.Inventory.Empty && base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
    }
}