using System.Diagnostics.CodeAnalysis;
using FoodShelves;

namespace MadHoarding;

public class BEMHFruitBasket : BEMHBasket
{
    protected override string CeilingAttachedUtil => ShapeReferences.utilFruitBasket;
    protected override string CantPlaceMessage => "foodshelves:Only fruit can be placed in this basket.";
    protected override InfoDisplay.InfoDisplayOptions InfoDisplay => FoodShelves.InfoDisplay.InfoDisplayOptions.ByBlockAverageAndSoonest;

    public override int ItemsPerSegment => 22;
    public override string AttributeCheck => "fsFruitBasket";
    public override int ItemsPerStack  => CalculateItemsPerStack(24);

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public BEMHFruitBasket() { inv = new MHInventory(SlotCount, InventoryClassName + "-0", Api,
        (_, inventory) => new ItemSlotFSUniversal(inventory, AttributeCheck, ItemsPerStack)); }
}