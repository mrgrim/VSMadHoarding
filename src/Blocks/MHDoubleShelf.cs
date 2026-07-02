using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MadHoarding;

public class MHDoubleShelf : BlockDoubleShelf
{
    public WorldInteraction[]? interactions;

    protected virtual string InteractionsName => "HolderUniversal";

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        interactions = ObjectCacheUtil.GetOrCreate(api, $"DoubleShelfBlockInteractions", () =>
        {
            List<ItemStack> stackList = [];

            foreach (var item in api.World.Items)
            {
                if (item.Code == null) continue;

                if (item.CanStoreInSlot("fs" + InteractionsName))
                    stackList.Add(new ItemStack(item));
            }

            return new WorldInteraction[]
            {
                new()
                {
                    ActionLangCode = "blockhelp-shelf-place",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null,
                    Itemstacks = [.. stackList]
                }
            };
        });
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        var be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEMHDoubleShelf;
        var ret = (BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? []).Append(interactions);

        if (be is null) return ret;

        var startIndex = selection.SelectionBoxIndex * be.ItemsPerSegment;
        var inv = be.Inventory;

        if (startIndex >= inv.Count)
            return ret;

        if (inv[startIndex].Itemstack?.Collectible is BaseFSContainer)
        {
            ret = [.. ret, new WorldInteraction
                {
                    ActionLangCode = "blockhelp-shelf-take",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift",
                    Itemstacks = []
                },
                .. (inv[startIndex].Itemstack?.Collectible as IContainedInteractable)?
                   .GetContainedInteractionHelp(be, inv[startIndex], forPlayer, selection) ?? []];
        }
        else
        {
            ret = [.. ret, new WorldInteraction 
            {
                ActionLangCode = "blockhelp-shelf-take",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = null,
            }];
        }

        return ret;
    }
}