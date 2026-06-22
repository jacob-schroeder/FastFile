using FastFile.ModelsOLD.Data;
using System.Globalization;
using FastFile.ModelsOLD.Zone;
using UI.Models;

namespace UI.Views.Assets;

internal static class AssetViewFormatters
{
    public const string OffsetPointerText = "[OFFSET]";
    public const string NullPointerText = "[NULL]";
    public const string UnresolvedPointerText = "[UNRESOLVED]";

    public static string FormatPointer(Pointer? pointer)
    {
        return pointer?.Kind switch
        {
            PointerKind.Offset => OffsetPointerText,
            PointerKind.Null => NullPointerText,
            PointerKind.Inline => "Inline",
            PointerKind.Insert => "Insert",
            null => NullPointerText,
            _ => UnresolvedPointerText
        };
    }

    public static string FormatPointerRaw(Pointer? pointer)
    {
        return pointer is null
            ? NullPointerText
            : $"0x{pointer.Raw:X8} ({FormatPointer(pointer)})";
    }

    public static KeyValueListItem PointerItem(string key, Pointer? pointer)
    {
        return new KeyValueListItem(
            key,
            FormatPointerRaw(pointer),
            BlockStreamNavigationTarget.FromPointer(pointer));
    }

    public static BlockStreamNavigationTarget? GetNavigationTarget(Pointer? pointer)
    {
        return BlockStreamNavigationTarget.FromPointer(pointer);
    }

    public static string FormatByte(byte value)
    {
        return value.ToString(CultureInfo.CurrentCulture);
    }
}
