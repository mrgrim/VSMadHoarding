using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

// ReSharper disable once CheckNamespace
namespace MadHoarding;

public abstract class BEMHBasket : BEBaseFSBasket
{
    public abstract int ItemsPerStack { get; }
    
    public AssetLocation? LockedCode { 
        get;
        set
        {
            if (field?.Equals(value) ?? false) return;

            field = value;
            MarkMeshesDirty();
            
            // This will be called from FromTreeAttributes which is before this classes own Api field is set, use the ModSystem static
            LockedCollectible = value is null ? null :
                MadHoarding.Api?.World.GetItem(value) as CollectibleObject ??
                MadHoarding.Api?.World.GetBlock(value) ?? null;

            if (value is null || LockedCollectible is null) return;
            
            // Stack count _or_ items per stack can change here, especially with modded items that might have varying stack sizes.
            (inv as MHInventory)?
                .Resize(SlotCount, (_, inventory) => new ItemSlotFSUniversal(inventory, AttributeCheck, ItemsPerStack));
        }
    }
    
    public CollectibleObject? LockedCollectible { get; set; }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        
        if (!inv.Empty && LockedCollectible is null
                       && inv.FirstOrDefault(s => s.Itemstack?.Collectible?.CanStoreInSlot(AttributeCheck) ?? false) is { } invSlot)
            LockedCode = invSlot.Itemstack?.Collectible.Code;
    }
    
    public int CalculateItemsPerStack(int defaultItemsPerStack) =>
        LockedCollectible is null ? defaultItemsPerStack : (int)Math.Ceiling(LockedCollectible.MaxStackSize * 8.0 / ItemsPerSegment);

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel, string? overrideAttrCheck = null) {
        var slot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var shift = byPlayer.Entity.Controls.ShiftKey;

        switch (shift)
        {
            // Take basket - falls back to base block class behavior checks
            case true when slot.Empty:
                return false;
            case false:
                return TryTake(byPlayer, blockSel);
            case true when slot.CanStoreInSlot(overrideAttrCheck ?? AttributeCheck) && TryPut(byPlayer, slot, blockSel):
                return this.HandlePlacementEffects(slot.Itemstack, byPlayer);
        }

        if (CantPlaceMessage != "") {
            (Api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get(CantPlaceMessage));
        }

        return true;
    }

    protected override bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel)
    {
        // Handle baskets that have no LockedCode but contain an inventory. This will happen when this mod is added to a world
        // already populated with Food Shelves baskets with inventories.
        if (LockedCollectible is null)
            if (inv.FirstOrDefault(s => s.Itemstack?.Collectible?.CanStoreInSlot(AttributeCheck) ?? false) is { } invSlot)
                LockedCode = invSlot.Itemstack?.Collectible.Code;
            else if (slot.Itemstack?.Collectible is { } collectible && collectible.CanStoreInSlot(AttributeCheck))
                LockedCode = collectible.Code;

        if (LockedCollectible?.Id == (slot.Itemstack?.Collectible?.Id ?? -1) 
            && LockedCollectible?.ItemClass == slot.Itemstack?.Collectible?.ItemClass
            && base.TryPut(byPlayer, slot, blockSel)) 
            return true;

        if (inv.Empty) LockedCode = null;
        return false;
    }

    // I think this is how it should look in the base class tbh. TODO: contact SONZ-INA about it.
    public override int CountItemsInSegment(int startIndex) {
        var count = 0;

        for (var i = 0; i < ItemsPerSegment; i++)
            if (inv[startIndex + i].StackSize >= inv[startIndex + i].MaxSlotStackSize)
                count++;

        return count;
    }
    
    // Same as above
    protected override int TryPutIntoSegment(ItemSlot slot, int startIndex, bool ctrl)
    {
        var moved = 0;
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var source = slot.Itemstack!;
        
        for (var i = 0; i < ItemsPerSegment; i++) {
            var target = inv[startIndex + i];

            // ReSharper disable once NullableWarningSuppressionIsUsed
            if (!target.Empty && target.Itemstack!.Collectible.Id != source.Collectible.Id) continue;
            
            var fsSlot = (ItemSlotFSUniversal)target;
            var available = fsSlot.GetRemainingSlotSpace(source);
            if (available == 0) continue;

            moved += slot.TryPutIntoBulk(Api.World, target, ctrl ? available : 1);
            
            // If we want ctrl to empty the source stack.
            // I think it feels better if ctrl click removes one "visual unit".
            // if (ctrl && slot.StackSize > 0) continue; 

            break;
        }

        return moved;
    }

    protected override bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        var ret = base.TryTake(byPlayer, blockSel);
        
        if (inv.Empty) LockedCode = null;
        if (ret) MarkDirty(); // TODO: This missing seems to be a bug in Food Shelves, remove when fixed. (is it?)
        return ret;
    }

    protected override ItemStack? TryTakeFromSegment(IPlayer byPlayer, int startIndex)
    {
        var result = (inv as MHInventory)?
            .GetLastOrMostTransitioned((Block as ITransitionContainer)?.TransitionItemsRef, startIndex, ItemsPerSegment) is { } itemSlot
                ? itemSlot.TakeOut(byPlayer.Entity.Controls.CtrlKey ? itemSlot.StackSize : 1) : null;
        
        if (result == null) return result;
        (inv as MHInventory)?.Compress();

        return result;
    }
    
    public override void OnBlockPlaced(ItemStack byItemStack)
    {
        if (byItemStack.Attributes["FSLockedCode"] is { } attribute)
            LockedCode = new AssetLocation(attribute.ToString());
        base.OnBlockPlaced(byItemStack);
    }
    
    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        if (LockedCode is not null) tree.SetString("LockedCode", LockedCode.ToString());
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        if (tree.GetString("LockedCode") is { } lockedCode) LockedCode = new AssetLocation(lockedCode);
        base.FromTreeAttributes(tree, worldAccessForResolve);
    }
}