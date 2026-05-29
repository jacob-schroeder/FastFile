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
            Menus = Memory.ReadPointer<ZonePointer<MenuDef[]>>(span, ref position),
        };
        
        ResolveName(ref asset, span, ref position);
        
        return asset;
    }

    private static void ResolveName(ref MenuList asset, ReadOnlySpan<byte> span, ref int position)
    {
        asset.NamePtr.SetResult(span.ReadCStringAt(ref position));
    }
}
