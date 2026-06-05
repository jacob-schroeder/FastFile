using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class MenufileReader
{
    public static BaseAsset Read(ref XFileReadContext context)
    {
        var asset = new MenuList
        {
            Offset = context.Position,
        };

        asset.NamePtr = context.ReadDirectPointer<string>("MenuList.Name");
        asset.MenuCount = context.ReadInt32();
        var menusPointer = context.ReadDirectPointer<ZonePointer<MenuDef>[]>("MenuList.Menus");

        if (asset.MenuCount is < 0 or > 10_000
            || asset.MenuCount == 0 && (asset.NamePtr.Kind != PointerKind.Null || menusPointer.Kind != PointerKind.Null))
        {
            context.Position = asset.Offset;
            return MenuReader.Read(ref context, windowDynamicFlagCount: 2);
        }

        context.ResolveInlinePointer(asset.NamePtr, GenericReader.ReadStringPointerValue);
        context.ResolveInlinePointer(
            menusPointer,
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<MenuDef>[]> pointer) =>
            {
                var pointers = new ZonePointer<MenuDef>[asset.MenuCount];
                for (var i = 0; i < asset.MenuCount; i++)
                    pointers[i] = pointerContext.ReadAliasPointer<MenuDef>($"MenuList.Menus[{i}]");

                pointer.SetResult(pointers);

                foreach (var menuPointer in pointers)
                {
                    pointerContext.ResolvePointerInBlock(
                        menuPointer,
                        XFILE_BLOCK.TEMP,
                        (ref XFileReadContext menuContext, ZonePointer<MenuDef> resolvedMenuPointer) =>
                        {
                            var value = menuContext.ReadPointerValue(resolvedMenuPointer, MenuReader.Read);
                            resolvedMenuPointer.SetResult(value);
                        });
                }
            });
        asset.Menus = menusPointer;

        return asset;
    }
}
