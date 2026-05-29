using FastFile.Logic.Extensions;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;

namespace FastFile.Logic.Assets;

public static class MenufileReader
{
    public static MenuList Read(ReadOnlySpan<byte> span, ref int position)
    {
        var asset = new MenuList()
        {
            Offset = position,
            NamePtr = Memory.ReadPointer<string>(span, ref position),
            MenuCount = span.ReadInt32(ref position),
            Menus = Memory.ReadPointer<ZonePointer<MenuDef>[]>(span, ref position),
        };
        
        ResolveName(ref asset, span, ref position);
        
        ResolveMenus(ref asset, span, ref position);

        return asset;
    }

    private static void ResolveName(ref MenuList asset, ReadOnlySpan<byte> span, ref int position)
    {
        Memory.ResolvePointer(asset.NamePtr, position);
        position = asset.NamePtr.Offset;
        
        asset.NamePtr.SetResult(span.ReadCStringAt(ref position));
    }

    private static void ResolveMenus(ref MenuList asset, ReadOnlySpan<byte> span, ref int position)
    {
        Memory.ResolvePointer(asset.Menus, position);
        position = asset.Menus.Offset;

        ZonePointer<MenuDef>[] pointers = new ZonePointer<MenuDef>[asset.MenuCount];
        for (int i = 0; i < asset.MenuCount; i++)
            pointers[i] = Memory.ReadPointer<MenuDef>(span, ref position);
        asset.Menus.SetResult(pointers);

        for (int i = 0; i < asset.MenuCount; i++)
        {
            var menu = MenuReader.Read(span, ref position);

            //throwing a break b/c menu is not complete yet.
            return;
            
            throw new NotImplementedException();
        }
    }
}
