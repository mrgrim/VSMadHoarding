using System.Diagnostics.CodeAnalysis;
using FoodShelves;

// ReSharper disable once CheckNamespace
namespace MadHoarding;

public class BEMHVegetableBasket : BEMHBasket
{
    protected override string CeilingAttachedUtil => ShapeReferences.utilVegetableBasket;
    protected override string CantPlaceMessage => "foodshelves:Only vegetables can be placed in this basket.";
    protected override InfoDisplay.InfoDisplayOptions InfoDisplay => FoodShelves.InfoDisplay.InfoDisplayOptions.ByBlockAverageAndSoonest;
    public override string AttributeCheck => "fsVegetableBasket";

    public override int ItemsPerSegment => LockedCode is null ? 36 : MHVegetableBasket.StackCountByGrouping(LockedCode);
    public override int ItemsPerStack => CalculateItemsPerStack(36);

    [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor")]
    public BEMHVegetableBasket() { inv = new MHInventory(SlotCount, InventoryClassName + "-0", Api,
        (_, inventory) => new ItemSlotFSUniversal(inventory, AttributeCheck, ItemsPerStack)); }

    protected override string? GetTransformationPath() => LockedCode?.ToString();
}