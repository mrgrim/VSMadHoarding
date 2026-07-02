using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

// ReSharper disable once CheckNamespace
namespace MadHoarding;

// Two things make this necessary:
//     - No public interface to change stackCountLimit in ItemSlotFSUniversal.
//     - No public interface to change SlotCount in InventoryGeneric, which is made more difficult by it being an array.

public class MHInventory : InventoryGeneric
{
    public MHInventory(ICoreAPI api)
        : base(api)
    {
    }
    
    public MHInventory(
        int quantitySlots,
        string invId,
        ICoreAPI api,
        NewSlotDelegate? onNewSlot = null)
        : base(quantitySlots, invId, api, onNewSlot)
    {
    }

    public MHInventory(
        int quantitySlots,
        string className,
        string instanceId,
        ICoreAPI api,
        NewSlotDelegate? onNewSlot = null)
        : base(quantitySlots, className, instanceId, api, onNewSlot)
    {
    }
    
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "onNewSlot")]
    private static extern ref NewSlotDelegate onNewSlot(InventoryGeneric _);
    
    // This will truncate an inventory when reducing size, be careful!
    public void Resize(int slotCount, NewSlotDelegate? onNewSlot = null)
    {
        if (onNewSlot is not null)
            MHInventory.onNewSlot(this) = onNewSlot;
            
        var invTree = new TreeAttribute();
        SlotsToTreeAttributes(slots, invTree);

        // Replaces the array and generates all new slot objects. We do this because we accept a new delegate.
        GenEmptySlots(slotCount);
        slots = SlotsFromTreeAttributes(invTree);
    }

    public ItemSlot? GetLastOrMostTransitioned(
        Dictionary<(int id, EnumItemClass type), int>? transitionItems,
        int startIdx = 0,
        int? endIdx = null)
    {
        return GetLastOrMostTransitioned(transitionItems, slots, Api, startIdx, endIdx);
    }

    public static ItemSlot? GetLastOrMostTransitioned(
        Dictionary<(int id, EnumItemClass type), int>? transitionItems,
        ReadOnlySpan<ItemSlot> slots,
        ICoreAPI api,
        int startIdx = 0,
        int? numSlots = null)
    {
        transitionItems ??= new Dictionary<(int id, EnumItemClass type), int>();
        (int idx, float progress, int depth) mostTransitioned = (-1, 0f, 0);

        if (startIdx + (numSlots ?? slots.Length) > slots.Length)
        {
            api.Logger.Error(
                $"[Mad Hoarding] GetLastOrMostTransitioned ask to exceed inventory slot count ({startIdx},{numSlots},{slots.Length}). " +
                " Reducing to avoid a crash, but might cause unexpected behavior. Please report!");
            numSlots = slots.Length - startIdx;
        }
        
        for (var idx = startIdx + (numSlots ?? slots.Length) - 1; idx >= startIdx; idx--) {
            var collectible = slots[idx].Itemstack?.Collectible;

            if (slots[idx].Empty || collectible is null) continue;

            // Grab first non-empty.
            if (mostTransitioned.idx == -1) mostTransitioned = (idx, 0f, 0);
            
            // But keep looking for transitionables.
            if (transitionItems.TryGetValue((collectible.Id, collectible.ItemClass), out var depth))
                if (depth > mostTransitioned.depth)
                    mostTransitioned = (idx, 0f, depth);
            
            foreach (var transitionState in collectible.UpdateAndGetTransitionStates(api.World, slots[idx]) ?? [])
                if (transitionState.TransitionedHours > mostTransitioned.progress && depth == mostTransitioned.depth)
                    mostTransitioned = (idx, transitionState.TransitionedHours, mostTransitioned.depth);
        }
        
        return mostTransitioned.idx == -1 ? null : slots[mostTransitioned.idx];
    }
    
    public void Compress()
    {
        if (slots is null || slots.Length <= 1) return;

        var writeIdx = 0;

        for (var readIdx = 0; readIdx < slots.Length; readIdx++)
        {
            if (slots[readIdx].Empty) continue;
            
            if (readIdx != writeIdx)
                (slots[writeIdx], slots[readIdx]) = (slots[readIdx], slots[writeIdx]);
            
            writeIdx++;
        }
    }
    
    public static void InitializeTransitions(ICoreAPI api, string whitelist,  Dictionary<(int id, EnumItemClass type), int> transitionItems)
    {
        List<(int id, EnumItemClass type)> whitelistedLocations =
            (from collectible in api.World.Collectibles
                                 ?? throw new InvalidOperationException("[Mad Hoarding] API or World is null")
                where collectible.Attributes?[whitelist].AsBool() ?? false
                select (collectible.Id, collectible.ItemClass)).ToList();

        findTransitions(whitelistedLocations, 1);
        return;
        
        void findTransitions(List<(int id, EnumItemClass type)> items, int depth)
        {
            if (depth > 5) return; // Props to any mod that makes this value too low.
            
            foreach (var code in items)
            {
                var collectible =
                    (code.type == EnumItemClass.Item ? api.World.GetItem(code.id) as CollectibleObject : api.World.GetBlock(code.id))
                    ?? throw new InvalidOperationException($"[Mad Hoarding] Collectible not found for code: {code.id}");
            
                var transitionProps = collectible.TransitionableProps ?? [];
        
                foreach (var transitionProp in transitionProps)
                    if (transitionProp?.TransitionedStack.Code is { } tcode)
                        if ((api.World.GetBlock(tcode) as CollectibleObject ?? api.World.GetItem(tcode)) is { } tcollectible)
                            transitionItems[(tcollectible.Id, tcollectible.ItemClass)] = depth;
            }
            
            // Oh no _RECURSION_ everyone RUN FOR YOUR LIVES!!!
            findTransitions(transitionItems
                .Where(x => x.Value == depth)
                .Select(x => x.Key)
                .ToList(), depth + 1);
        }
    }
}