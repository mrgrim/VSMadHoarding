using FoodShelves;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MadHoarding;

public class MHCoolingCabinet : BlockCoolingCabinet
{
    private WorldInteraction[]? doorOpenClose;
    private WorldInteraction[]? drawerOpenClose;
    private WorldInteraction[]? drawerInteractions;

    public override void OnLoaded(ICoreAPI api) {
        base.OnLoaded(api);

        doorOpenClose = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetCabinetInteractions", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right
                }
            };
        });

        drawerOpenClose = ObjectCacheUtil.GetOrCreate(api, "drawerOpenClose", () => {
            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-door-openclose",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                }
            };
        });

        itemSlottableInteractions = ObjectCacheUtil.GetOrCreate(api, "MHcoolingCabinetItemInteractions", () => {
            List<ItemStack> holderUniversalStackList = [];

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot("fsHolderUniversal")) {
                    holderUniversalStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-add",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. holderUniversalStackList]
                }
            };
        });

        drawerInteractions = ObjectCacheUtil.GetOrCreate(api, "coolingCabinetDrawerInteractions", () => {
            List<ItemStack> coolingOnlyStackList = [];

            foreach (var obj in api.World.Collectibles) {
                if (obj.CanStoreInSlot(Constants.FSCoolingOnly)) {
                    coolingOnlyStackList.Add(new ItemStack(obj));
                }
            }

            return new WorldInteraction[] {
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addone",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. coolingOnlyStackList]
                },
                new() {
                    ActionLangCode = "blockhelp-groundstorage-addbulk",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = [.. coolingOnlyStackList],
                    HotKeyCode = "ctrl",
                }
            };
        });
    }

    public override WorldInteraction[]? GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        if (world.BlockAccessor.GetBlockEntity(selection.Position) is not BECoolingCabinet becc) return doorOpenClose;
        
        switch (selection.SelectionBoxIndex)
        {
            case < 9:
            {
                if (becc.DoorOpen) {
                    var ret = (BaseGetPlacedBlockInteractionHelp(world, selection, forPlayer) ?? []).Append(itemSlottableInteractions);

                    var startIndex = selection.SelectionBoxIndex * becc.ItemsPerSegment;
                    var inv = becc.Inventory;

                    if (startIndex >= inv.Count)
                        return ret;

                    if (inv[startIndex].Itemstack?.Collectible is BaseFSContainer)
                    {
                        ret = [.. ret, new WorldInteraction
                            {
                                ActionLangCode = "blockhelp-shelf-take",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCode = "shift",
                                Itemstacks = null 
                            },
                            .. (inv[startIndex].Itemstack?.Collectible as IContainedInteractable)?
                            .GetContainedInteractionHelp(becc, inv[startIndex], forPlayer, selection) ?? []];
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

                break;
            }
            case 11 when becc.DrawerOpen:
                return drawerOpenClose.Append(drawerInteractions);
            case 11:
                return drawerOpenClose;
        }

        return doorOpenClose;
    }
}