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
        
        api.RegisterBlockEntityClass("MadHoarding.BEMHVegetableBasket", typeof(BEMHVegetableBasket));
        api.RegisterBlockEntityClass("MadHoarding.BEMHFruitBasket", typeof(BEMHFruitBasket));
        api.RegisterBlockEntityClass("MadHoarding.BEMHMushroomBasket", typeof(BEMHMushroomBasket));
        api.RegisterBlockEntityClass("MadHoarding.BEMHEggBasket", typeof(BEMHEggBasket));
        api.RegisterBlockEntityClass("MadHoarding.BEMHDoubleShelf", typeof(BEMHDoubleShelf));
        api.RegisterBlockEntityClass("MadHoarding.BEMHCoolingCabinet", typeof(BEMHCoolingCabinet));
        
        api.RegisterBlockClass("MadHoarding.MHVegetableBasket", typeof(MHVegetableBasket));
        api.RegisterBlockClass("MadHoarding.MHFruitBasket", typeof(MHFruitBasket));
        api.RegisterBlockClass("MadHoarding.MHMushroomBasket", typeof(MHMushroomBasket));
        api.RegisterBlockClass("MadHoarding.MHEggBasket", typeof(MHEggBasket));
        api.RegisterBlockClass("MadHoarding.MHDoubleShelf", typeof(MHDoubleShelf));
        api.RegisterBlockClass("MadHoarding.MHCoolingCabinet", typeof(MHCoolingCabinet));
        
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