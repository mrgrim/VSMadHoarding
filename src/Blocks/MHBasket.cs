using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

// ReSharper disable once CheckNamespace
namespace MadHoarding;

public interface ITransitionContainer
{
    // This is likely to only ever be a handful of items. A List<int id, EnumItemClass type, int depth)> that is simply searched
    // on each lookup would likely outperform this Dictionary in synthetic tests by a considerable amount, but the total impact
    // to tick time is likely negligible either way.
    static abstract Dictionary<(int id, EnumItemClass type), int> TransitionItems { get; }
    public Dictionary<(int id, EnumItemClass type), int> TransitionItemsRef { get; }
}

public abstract class MHBasket<T> : BaseFSBasket, IContainedInteractable where T : MHBasket<T>, ITransitionContainer
{
    public WorldInteraction[]? interactions;
    
    public (string? code, CollectibleObject? collectible) GetLockedCode(
        ItemStack containerStack,
        CollectibleObject handCollectible,
        ItemStack?[] contents,
        string whitelist)
    {
        (string? code, CollectibleObject? collectible) lockedCode = (null, null);
        
        lockedCode.code = containerStack.Attributes?["FSLockedCode"]?.ToString() ?? null;

        if (lockedCode.code is null)
            if (contents.FirstOrDefault(stack => stack?.Collectible?.CanStoreInSlot(whitelist) ?? false) is { } contentStack)
            {
                lockedCode = (contentStack.Collectible.Code, contentStack.Collectible);
                containerStack.Attributes?.SetString("FSLockedCode", lockedCode.code);
            }
            else
                lockedCode = (handCollectible.Code, handCollectible);
            
        if (lockedCode.code == null) return (null, null);

        lockedCode.collectible ??= api.World.GetItem(lockedCode.code) as CollectibleObject ?? 
                                   api.World.GetBlock(lockedCode.code);
        
        return lockedCode;
    }

    public virtual (int slotCount, int stackSize, string? lockedCode, CollectibleObject? lockedCollectible) GetBasketProperties(
        ItemStack containerStack,
        CollectibleObject handCollectible,
        ItemStack?[] contents,
        string whitelist)
    {
        var lockedCode = GetLockedCode(containerStack, handCollectible, contents, whitelist);
        
        return (
            InnerSlotCount, 
            lockedCode.collectible is null ? 64 : (int)Math.Ceiling(lockedCode.collectible.MaxStackSize * 8.0 / InnerSlotCount),
            lockedCode.code,
            lockedCode.collectible);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel) =>
        (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BEBaseFSBasket frbasket && frbasket.OnInteract(byPlayer,blockSel))
        || BaseOnBlockInteractStart(world, byPlayer, blockSel);
    
    public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        var handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        var whitelist = $"fs{InteractionsName}";
        var containerStack = slot.Itemstack;

        if (containerStack is null) return false;
            
        var contents = GetContents(api.World, containerStack);
        if (contents is null) return false;

        var shift = byPlayer.Entity.Controls.ShiftKey;
            
        // Putting stuff in
        if (shift && !handSlot.Empty && handSlot.CanStoreInSlot(whitelist))
        {
            if (handSlot.Itemstack?.Collectible is not { } handCollectible) return false;

            var props = GetBasketProperties(containerStack, handCollectible, contents, whitelist);
            
            if (props.lockedCode is null) return false;
            if (props.lockedCollectible?.Id != handSlot.Itemstack.Collectible.Id ||
                props.lockedCollectible?.ItemClass != handSlot.Itemstack.Collectible.ItemClass) return false;

            var moved = 0;
            var ctrl = byPlayer.Entity.Controls.CtrlKey;
            
            for (var i = 0; i < props.slotCount; i++)
            {
                if (i == contents.Length) contents = [.. contents, null];
                
                var target = contents[i];
                var targetSlot = new DummySlot(target);

                if (target is not null && target.StackSize >= props.stackSize) continue;
                
                var op = new ItemStackMoveOperation(
                    api.World,
                    EnumMouseButton.Left,
                    0,
                    EnumMergePriority.DirectMerge,
                    ctrl ? props.stackSize - (target?.StackSize ?? 0) : 1);
                
                // You actually want to send this a null Itemstack rather than init one yourself.
                handSlot.TryPutInto(targetSlot, ref op);
                
                moved += op.MovedQuantity;
                contents[i] = targetSlot.Itemstack;
            
                // If we want ctrl to empty the hand stack rather than fill or top off one target stack.
                // I think it feels better if ctrl click fills one "visual unit", maybe?
                // if (ctrl && handSlot.StackSize > 0) continue; 
                if (op.MovedQuantity <= 0) continue;

                break;
            }

            if (moved <= 0) return false;
            
            containerStack.Attributes?.SetString("FSLockedCode", props.lockedCode);
            InventoryExtensions.SetContents(slot.Itemstack, contents);
            handSlot.MarkDirty();
            be.HandlePlacementEffects(contents.Last(), byPlayer);
            be.MarkDirty();
            
            return true;
        }

        // Taking stuff out
        if (!shift)
        {
            if (contents.Length == 0) return false;

            var taken = MHInventory.GetLastOrMostTransitioned(T.TransitionItems, 
                contents.Select(stack => new DummySlot(stack)).ToArray(), api);
            if (taken is null) return false;
            
            var xfer = taken.TakeOut(byPlayer.Entity.Controls.CtrlKey ? taken.StackSize : 1);
            
            // Compress the inventory removing empty slots.
            if (taken.StackSize == 0)
                contents = contents.Where(itemStack => (itemStack?.StackSize ?? 0) > 0).ToArray();

            if (!byPlayer.InventoryManager.TryGiveItemstack(xfer, true))
                if (api.Side == EnumAppSide.Server) api.World.SpawnItemEntity(xfer, byPlayer.Entity.Pos.XYZ);

            if (contents.Length == 0)
                containerStack.Attributes?.RemoveAttribute("FSLockedCode");
                    
            InventoryExtensions.SetContents(slot.Itemstack, contents);
            be.HandlePlacementEffects(handSlot.Itemstack, byPlayer);
            be.MarkDirty();
            return true;
        }

        return false;
    }
    
    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos) {
        var stack = base.OnPickBlock(world, pos);

        if (world.BlockAccessor.GetBlockEntity(pos) is BEMHVegetableBasket { LockedCode: not null } basket)
            stack.Attributes.SetString("FSLockedCode", basket.LockedCode.ToString());

        return stack;
    }
    
    public override void OnLoaded(ICoreAPI _api) {
        base.OnLoaded(_api);

        interactions = ObjectCacheUtil.GetOrCreate(api, InteractionsName + "BlockInteractions", Array.Empty<WorldInteraction>);
        
        var computedItemStacks = interactions.Length >= 1 ? interactions[0].Itemstacks : null;

        if (computedItemStacks is null)
        {
            var itemStackList = new List<ItemStack>();
            foreach (var item in api.World.Items)
            {
                if (item.Code == null) continue;

                if (item.CanStoreInSlot("fs" + InteractionsName))
                    itemStackList.Add(new ItemStack(item));
            }
            computedItemStacks = [.. itemStackList];
        }

        ObjectCacheUtil.Delete(api, InteractionsName + "BlockInteractions");
        interactions = [
                new WorldInteraction {
                    ActionLangCode = "blockhelp-groundstorage-addone",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = [.. computedItemStacks]
                },
                new WorldInteraction {
                    ActionLangCode = "blockhelp-groundstorage-removeone",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                },
                new WorldInteraction {
                    ActionLangCode = "blockhelp-groundstorage-addbulk",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCodes = [ "ctrl", "shift" ],
                    Itemstacks = [.. computedItemStacks]
                },
                new WorldInteraction {
                    ActionLangCode = "blockhelp-groundstorage-removebulk",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "ctrl"
                }
        ];
        ObjectCacheUtil.GetOrCreate(api, InteractionsName + "BlockInteractions",
            () => interactions);
    }
    
    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) 
        => (BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? []).Append(interactions);

    public new WorldInteraction[]? GetContainedInteractionHelp(
        BlockEntityContainer be,
        ItemSlot slot,
        IPlayer byPlayer,
        BlockSelection blockSel)
    {
        return interactions;
    }
}