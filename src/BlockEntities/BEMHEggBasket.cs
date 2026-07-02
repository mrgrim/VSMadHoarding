using System.Diagnostics.CodeAnalysis;
using FoodShelves;

namespace MadHoarding;

public class BEMHEggBasket : BEMHBasket
{
    protected override string CeilingAttachedUtil => ShapeReferences.utilEggBasket;
    protected override string CantPlaceMessage => "foodshelves:Only eggs can be placed in this basket.";
    protected override InfoDisplay.InfoDisplayOptions InfoDisplay => FoodShelves.InfoDisplay.InfoDisplayOptions.ByBlockAverageAndSoonest;
    
    public override int ItemsPerSegment => 12;
    public override string AttributeCheck => "fsEggBasket";
    public override int ItemsPerStack  => CalculateItemsPerStack(22);

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public BEMHEggBasket() { inv = new MHInventory(SlotCount, InventoryClassName + "-0", Api,
        (_, inventory) => new ItemSlotFSUniversal(inventory, AttributeCheck, ItemsPerStack)); }
}