using Avalonia.Media;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using System;
using System.Globalization;

namespace UI.Components.Menu;

internal static class MenuDisplayFormatter
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

    public static string FormatStringPointer(ZonePointer<string>? pointer, string? value, string emptyValue = "")
    {
        if (pointer is { Kind: PointerKind.Null })
        {
            return emptyValue;
        }

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return pointer is { Kind: PointerKind.Offset }
            ? OffsetPointerText
            : string.IsNullOrWhiteSpace(value)
            ? emptyValue
            : value;
    }

    public static string FormatAssetPointer<TAsset>(ZonePointer<TAsset>? pointer) where TAsset : BaseAsset
    {
        if (pointer is null)
        {
            return NullPointerText;
        }

        if (pointer.Kind == PointerKind.Null)
        {
            return NullPointerText;
        }

        return pointer.Result is { } asset && !string.IsNullOrWhiteSpace(asset.GetDisplayName)
            ? asset.GetDisplayName
            : FormatPointer(pointer);
    }

    public static string FormatRectangle(RectangleDef? rect)
    {
        return rect is null
            ? string.Empty
            : string.Create(
                CultureInfo.CurrentCulture,
                $"{rect.X:0.##}, {rect.Y:0.##}, {rect.W:0.##}, {rect.H:0.##}");
    }

    public static string FormatVec4(Vec4? color)
    {
        return color is null
            ? string.Empty
            : string.Create(
                CultureInfo.CurrentCulture,
                $"{color.R:0.###}, {color.G:0.###}, {color.B:0.###}, {color.A:0.###}");
    }

    public static string FormatYesNo(int value)
    {
        return value == 0 ? "No" : "Yes";
    }

    public static IBrush ToBrush(Vec4? color, string fallback)
    {
        if (color is null || color.A <= 0)
        {
            return SolidColorBrush.Parse(fallback);
        }

        return new SolidColorBrush(Color.FromArgb(
            ToColorByte(color.A),
            ToColorByte(color.R),
            ToColorByte(color.G),
            ToColorByte(color.B)));
    }

    private static byte ToColorByte(float value)
    {
        return (byte)Math.Round(Math.Clamp(value, 0f, 1f) * 255);
    }
}
