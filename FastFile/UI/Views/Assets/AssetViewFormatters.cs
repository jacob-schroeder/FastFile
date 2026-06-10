using FastFile.Models.Data;
using System.Globalization;
using FastFile.Models.Zone;

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

    public static string FormatByte(byte value)
    {
        return value.ToString(CultureInfo.CurrentCulture);
    }
}
