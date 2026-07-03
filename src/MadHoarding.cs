using HarmonyLib;
using Vintagestory.API.Common;

namespace MadHoarding;

public class MadHoarding : ModSystem
{
    public static ICoreAPI?                   Api;
    private static readonly Harmony           _harmony = new ("org.gr1m.mods.vintagestory.MadHoarding.Harmony");
    private static bool                       _patched = false;

    public override void Dispose()
    {
        if (!_patched) return;
        _harmony.UnpatchAll();
        _patched = false;
    }
    
    public override void Start(ICoreAPI api) {
        base.Start(api);
        
        Api = api;
        
        api.RegisterBlockEntityClass("FoodShelves.BEVegetableBasket", typeof(BEMHVegetableBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEFruitBasket", typeof(BEMHFruitBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEMushroomBasket", typeof(BEMHMushroomBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEEggBasket", typeof(BEMHEggBasket));
        api.RegisterBlockEntityClass("FoodShelves.BEDoubleShelf", typeof(BEMHDoubleShelf));
        api.RegisterBlockEntityClass("FoodShelves.BECoolingCabinet", typeof(BEMHCoolingCabinet));
        
        api.RegisterBlockClass("FoodShelves.BlockVegetableBasket", typeof(MHVegetableBasket));
        api.RegisterBlockClass("FoodShelves.BlockFruitBasket", typeof(MHFruitBasket));
        api.RegisterBlockClass("FoodShelves.BlockMushroomBasket", typeof(MHMushroomBasket));
        api.RegisterBlockClass("FoodShelves.BlockEggBasket", typeof(MHEggBasket));
        api.RegisterBlockClass("FoodShelves.BlockDoubleShelf", typeof(MHDoubleShelf));
        api.RegisterBlockClass("FoodShelves.BlockCoolingCabinet", typeof(MHCoolingCabinet));
        
        api.RegisterBlockBehaviorClass("MadHoarding.ShiftRightClickPickupWhenEmpty", typeof(BlockBehaviorShiftRightClickPickupWhenEmpty));
        
        if (_patched) return;
        
        _harmony.PatchAll();
        _patched = true;
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        
        MHVegetableBasket.InitCompiledPatterns();
        
        MHInventory.InitializeTransitions(api, "fsvegetablebasket", MHVegetableBasket.TransitionItems);
        MHInventory.InitializeTransitions(api, "fsfruitbasket", MHFruitBasket.TransitionItems);
        MHInventory.InitializeTransitions(api, "fsmushroombasket", MHMushroomBasket.TransitionItems);
        MHInventory.InitializeTransitions(api, "fseggbasket", MHEggBasket.TransitionItems);
    }
}