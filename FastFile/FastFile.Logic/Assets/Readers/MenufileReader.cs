using FastFile.Logic.Extensions;
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
        if (!LooksLikeMenuList(context))
            return MenuReader.Read(ref context, windowDynamicFlagCount: 2);

        var asset = new MenuList
        {
            Offset = context.Position,
        };

        asset.NamePtr = context.ReadDirectPointer<string>("MenuList.Name");
        asset.MenuCount = context.ReadInt32();
        var menusPointer = context.ReadDirectPointer<ZonePointer<MenuDef>[]>("MenuList.Menus");

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

    private static bool LooksLikeMenuList(XFileReadContext context)
    {
        var position = context.Position;
        if (position < 0 || position + 12 > context.Span.Length)
            return false;

        var nameRaw = context.Span.ReadInt32(ref position);
        var menuCount = context.Span.ReadInt32(ref position);
        var menusRaw = context.Span.ReadInt32(ref position);

        if (menuCount is < 0 or > 10_000)
            return false;

        if (menuCount != 0)
            return true;

        if (nameRaw == 0 && menusRaw == 0)
            return true;

        return IsInlinePointer(nameRaw)
            && IsInlinePointer(menusRaw)
            && LooksLikeInlineMenuListName(context, position);
    }

    private static bool IsInlinePointer(int raw)
    {
        return raw is -1 or -2;
    }

    private static bool LooksLikeInlineMenuListName(XFileReadContext context, int offset)
    {
        const int MaxMenuNameLength = 512;

        var span = context.Span;
        var end = Math.Min(span.Length, offset + MaxMenuNameLength);
        for (var i = offset; i < end; i++)
        {
            var value = span[i];
            if (value == 0)
                return i > offset;

            if (value < 0x20 || value > 0x7e)
                return false;
        }

        return false;
    }
}
