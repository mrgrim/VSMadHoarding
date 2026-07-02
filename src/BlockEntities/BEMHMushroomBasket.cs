using System.Diagnostics.CodeAnalysis;
using FoodShelves;

namespace MadHoarding;

public class BEMHMushroomBasket : BEMHBasket
{
    protected override string CeilingAttachedUtil => ShapeReferences.utilMushroomBasket;
    protected override string CantPlaceMessage => "foodshelves:Only mushrooms can be placed in this basket.";
    protected override InfoDisplay.InfoDisplayOptions InfoDisplay => FoodShelves.InfoDisplay.InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int ItemsPerSegment => 18;
    public override string AttributeCheck => "fsMushroomBasket";
    public override int ItemsPerStack  => CalculateItemsPerStack(29);
    
    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public BEMHMushroomBasket() { inv = new MHInventory(SlotCount, InventoryClassName + "-0", Api,
        (_, inventory) => new ItemSlotFSUniversal(inventory, AttributeCheck, ItemsPerStack)); }
}