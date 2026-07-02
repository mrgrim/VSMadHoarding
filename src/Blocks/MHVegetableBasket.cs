using System.Text.RegularExpressions;
using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

// ReSharper disable once CheckNamespace
namespace MadHoarding;

public class MHVegetableBasket : MHBasket<MHVegetableBasket>, ITransitionContainer
{
    public static Dictionary<(int id, EnumItemClass type), int> TransitionItems { get; } = [];
    public Dictionary<(int id, EnumItemClass type), int> TransitionItemsRef => TransitionItems;

    private static readonly ExplicitTransform LargeTransformations = new (
        X:  [ .17f,   0f,-.15f,  .1f,-.15f, .17f,-.15f,   0 ],
        Y:  [ .03f, .03f, .03f, .15f, .15f, .17f, .18f, .2f ],
        Z:  [ -.1f,  .1f,-.11f,  .1f,  .1f,-.11f,-.11f,   0 ],

        RX: [   -2,    0,    0,   15,   -3,    0,    0,  -2 ],
        RY: [    7,   -2,   15,  -10,   10,    0,   30,  12 ],
        RZ: [    1,   -1,    0,  -45,   25,  -20,   10,   3 ]
    );

    private static readonly ExplicitTransform MediumTransformations = new (
        X:  [    0,  .3f, .11f, .04f,-.22f,  .3f, -.11f, -.1f, .07f, .13f,-.07f,    0 ],
        Y:  [-.03f,-.03f,-.03f, .14f,    0, .11f,  .02f,  .1f, .38f,  .1f, .12f, .12f ],
        Z:  [-.15f,    0, .11f, .14f, .22f,-.25f, -.03f, -.1f,    0, .15f, .02f,    0 ],

        RX: [   -2,   0,    0,     3,   -3,   45,    16,    2,    0,    2,   -1,    1 ],
        RY: [    4,  -2,   10,    -4,  -95,    0,    20,   -1,   -2,    4,   40,  -90 ],
        RZ: [    1,  -1,    0,  -120,    1,    0,   -90,    1,  180,   -3,   -3,   -2 ]
    );

    private static readonly ExplicitTransform StandardTransformations = new(
        X:  [ .24f, .24f, .24f, .09f, .09f, .09f, -.09f,-.09f,-.09f,-.24f,-.24f,-.24f, .17f, .02f,-.07f, .12f, .17f,-.12f, -.3f,-.38f ],
        Y:  [-.04f,-.04f,-.04f,-.04f,-.04f,-.04f, -.04f,-.04f,-.04f,-.04f,-.04f,-.04f, .23f, .18f,  .1f, .23f, .23f, .16f, .18f, .23f ],
        Z:  [-.15f,    0, .15f,-.17f,    0, .15f, -.15f,    0, .15f,-.15f,    0, .15f, -.3f,  .1f,    0,-.17f, .12f,-.13f, .01f, .16f ],

        RX: [   -2,   0,    0,     3,   -3,    2,     3,    2,    0,    2,   -1,    1,   90,   89,    0,   90,   91,    0,   90,   90 ],
        RY: [    4,  -2,   -5,    -4,    0,    0,     4,   -1,   -2,    4,    0,   -1,    2,   20,   20,    0,    0,    0,   21,    1 ],
        RZ: [    1,  -1,    0,    -1,    1,    0,     0,    1,    0,   -3,   -3,   -2,   -4,  -91,  -40,   90,   78,   45,  -85,  -90 ]
    );

    private static readonly ExplicitTransform LongTransformations = new(
        X:  [ .18f,  .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f,  .18f,  .18f,  .18f,  .18f,  .18f,  .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f, .18f, .18f, .18f, .18f, .18f, .18f,-.12f,-.12f,-.12f,-.12f,-.12f,-.12f ],
        Y:  [    0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0, .065f, .065f, .065f, .065f, .065f, .065f,.065f,.065f,.065f,.065f,.065f,.065f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f, .13f ],
        Z:  [-.19f, -.12f,-.05f, .02f, .09f, .16f,-.18f,-.11f,-.04f, .03f,  .1f, .17f, -.18f, -.11f, -.04f,  .03f,   .1f,  .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f,-.18f,-.11f,-.04f, .03f,  .1f, .17f ],

        RX: [    0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,     0,     0,     0,     0,     0,     0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,    0 ],
        RY: [    4,     5,    4,    2,    3,    4,   -2,   -2,   -2,   -2,   -2,   -2,    -1,    -3,    -2,    -3,    -1,    -2,    4,    3,    4,    3,    2,    4,    4,    3,    5,    4,    3,    5,   -2,   -2,   -1,   -2,   -3,   -2 ],
        RZ: [    1,     2,    1,    0,    1,    2,    1,    3,    1,    2,    1,    0,     1,    -1,     1,     0,     1,     1,    0,    1,    1,    2,    1,    1,    1,    1,    1,    1,    1,    0,    1,    2,    1,    1,    0,    1 ]
    );
    
    public override int InnerSlotCount => 36;
    protected override string InteractionsName => "vegetablebasket";

    public static void InitCompiledPatterns()
    {
        if (CompiledPatterns.Count != 0) return;
        
        // ReSharper disable once NullableWarningSuppressionIsUsed
        foreach (var groupingCode in BlockVegetableBasket.VegetableBasketData.GroupingCodes!)
        foreach (var assetCodeWildcard in groupingCode.Value)
            CompiledPatterns.Add((
                Regex: new Regex(
                    // Converts e.g. "game:vegetable-*" to "^game:vegetable-.*$"
                    $"^{Regex.Escape(assetCodeWildcard).Replace("\\*", ".*").Replace("\\?", ".")}$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase
                ),
                Key: groupingCode.Key));
    }
    
    private static readonly List<(Regex Regex, string Key)> CompiledPatterns = []; 
    private static readonly Dictionary<string, (int slots, ExplicitTransform transform)> GroupSizeAttributes = new()
    {
        { "large", (8, LargeTransformations) }, { "medium", (12, MediumTransformations) },
        { "standard", (20, StandardTransformations) }, { "long", (36, LongTransformations) }
    };

    public static (Regex Regex, string Key)? GroupingSize(AssetLocation code) =>
        CompiledPatterns.FirstOrDefault(p => p.Regex.IsMatch(code.ToString()));

    public static int StackCountByGrouping(AssetLocation code) =>
        GroupingSize(code) is { Key: not null } match && GroupSizeAttributes.TryGetValue(match.Key, out var attr)
            ? attr.slots : 36;

    public override ExplicitTransform GetTransformationMatrix(string? path = null) =>
        path is not null && GroupingSize(path) is { Key: not null } match
                         && GroupSizeAttributes.TryGetValue(match.Key, out var attr)
            ? attr.transform : LongTransformations;

    public override Action<TransformationData> GetTransformationModifier() {
        return t => {
            t.scaleX = t.scaleY = t.scaleZ = 0.5f;
            t.offsetY = 0.015f;
        };
    }

    public override (int slotCount, int stackSize, string? lockedCode, CollectibleObject? lockedCollectible) GetBasketProperties(
        ItemStack containerStack,
        CollectibleObject handCollectible,
        ItemStack?[] contents,
        string whitelist)
    {
        var lockedCode = GetLockedCode(containerStack, handCollectible, contents, whitelist);
        var slotCount = lockedCode.code is null ? 36 : StackCountByGrouping(lockedCode.code);

        return (
            slotCount, 
            lockedCode.collectible is null ? 64 : (int)Math.Ceiling(lockedCode.collectible.MaxStackSize * 8.0 / slotCount),
            lockedCode.code,
            lockedCode.collectible);
    }

    protected override MeshData? GenBasketContents(ItemStack? itemstack, ITextureAtlasAPI targetAtlas)
    {
        if (itemstack == null) return null;
        
        var contents = GetContents(api.World, itemstack);
        
        var lockedCode = itemstack.Attributes?["FSLockedCode"]?.ToString() ?? null;

        lockedCode ??= contents.FirstOrDefault(stack =>
            stack?.Collectible?.CanStoreInSlot($"fs{InteractionsName}") ?? false)?.Collectible?.Code;
        if (lockedCode is not null) itemstack.Attributes?.SetString("FSLockedCode", lockedCode);

        return Meshing.GenContentMesh(api as ICoreClientAPI, contents, GetTransformationMatrix(lockedCode),
            Transformations, GetTransformationModifier());
    }
}