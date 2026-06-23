using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UI.Components.Menu;

internal static class MenuEnumFormatter
{
    private static readonly (int Value, string Name)[] DvarFlagNames =
    [
        (0x1, "ITEM_DVAR_FLAG_ENABLE"),
        (0x2, "ITEM_DVAR_FLAG_DISABLE"),
        (0x4, "ITEM_DVAR_FLAG_SHOW"),
        (0x8, "ITEM_DVAR_FLAG_HIDE"),
        (0x10, "ITEM_DVAR_FLAG_FOCUS")
    ];

    public static string FormatItemType(int value)
    {
        return FormatEnumValue(value, GetItemTypeName(value), "ITEM_TYPE_");
    }

    public static string FormatItemTypeCompact(int value)
    {
        return FormatEnumValue(value, GetItemTypeName(value), "ITEM_TYPE_", includeEnumName: false);
    }

    public static string FormatWindowStyle(int value)
    {
        return FormatEnumValue(value, GetWindowStyleName(value), "WINDOW_STYLE_");
    }

    public static string FormatWindowBorder(int value)
    {
        return FormatEnumValue(value, GetWindowBorderName(value), "WINDOW_BORDER_");
    }

    public static string FormatOwnerDraw(int value)
    {
        return FormatEnumValue(value, GetOwnerDrawName(value), "UI_OWNERDRAW_");
    }

    public static string FormatTextStyle(int value)
    {
        return FormatEnumValue(value, GetTextStyleName(value), "ITEM_TEXTSTYLE_");
    }

    public static string FormatTextAlignMode(int value)
    {
        return FormatEnumValue(value, GetTextAlignName(value), "ITEM_ALIGN_");
    }

    public static string FormatDvarFlags(int flags)
    {
        return FormatFlags(flags, DvarFlagNames, "ITEM_DVAR_FLAG_");
    }

    private static string FormatEnumValue(
        int value,
        string? enumName,
        string prefix,
        bool includeEnumName = true)
    {
        if (enumName is null)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        var displayName = ToDisplayName(enumName, prefix);
        return includeEnumName
            ? $"{displayName} ({enumName}, {value.ToString(CultureInfo.CurrentCulture)})"
            : $"{displayName} ({value.ToString(CultureInfo.CurrentCulture)})";
    }

    private static string FormatFlags(int flags, IEnumerable<(int Value, string Name)> names, string prefix)
    {
        if (flags == 0)
        {
            return "None (0x00000000)";
        }

        var remaining = flags;
        var displayNames = new List<string>();
        var enumNames = new List<string>();

        foreach (var (value, name) in names.Where(name => (flags & name.Value) == name.Value))
        {
            displayNames.Add(ToDisplayName(name, prefix));
            enumNames.Add(name);
            remaining &= ~value;
        }

        if (remaining != 0)
        {
            displayNames.Add($"Unknown 0x{remaining:X8}");
        }

        var enumSuffix = enumNames.Count == 0
            ? string.Empty
            : $" ({string.Join(" | ", enumNames)}, 0x{flags:X8})";
        return $"{string.Join(", ", displayNames)}{enumSuffix}";
    }

    private static string? GetItemTypeName(int value)
    {
        return value switch
        {
            0 => "ITEM_TYPE_TEXT",
            1 => "ITEM_TYPE_BUTTON",
            2 => "ITEM_TYPE_RADIOBUTTON",
            3 => "ITEM_TYPE_CHECKBOX",
            4 => "ITEM_TYPE_EDITFIELD",
            5 => "ITEM_TYPE_COMBO",
            6 => "ITEM_TYPE_LISTBOX",
            7 => "ITEM_TYPE_MODEL",
            8 => "ITEM_TYPE_OWNERDRAW",
            9 => "ITEM_TYPE_NUMERICFIELD",
            10 => "ITEM_TYPE_SLIDER",
            11 => "ITEM_TYPE_YESNO",
            12 => "ITEM_TYPE_MULTI",
            13 => "ITEM_TYPE_DVARENUM",
            14 => "ITEM_TYPE_BIND",
            15 => "ITEM_TYPE_VALIDFILEFIELD",
            16 => "ITEM_TYPE_DECIMALFIELD",
            17 => "ITEM_TYPE_UPREDITFIELD",
            18 => "ITEM_TYPE_GAME_MESSAGE_WINDOW",
            19 => "ITEM_TYPE_NEWSTICKER",
            20 => "ITEM_TYPE_TEXTSCROLL",
            21 => "ITEM_TYPE_EMAILFIELD",
            22 => "ITEM_TYPE_PASSWORDFIELD",
            _ => null
        };
    }

    private static string? GetWindowStyleName(int value)
    {
        return value switch
        {
            0 => "WINDOW_STYLE_EMPTY",
            1 => "WINDOW_STYLE_FILLED",
            2 => "WINDOW_STYLE_GRADIENT",
            3 => "WINDOW_STYLE_SHADER",
            4 => "WINDOW_STYLE_TEAMCOLOR",
            5 => "WINDOW_STYLE_CINEMATIC",
            _ => null
        };
    }

    private static string? GetWindowBorderName(int value)
    {
        return value switch
        {
            0 => "WINDOW_BORDER_NONE",
            1 => "WINDOW_BORDER_FULL",
            2 => "WINDOW_BORDER_HORZ",
            3 => "WINDOW_BORDER_VERT",
            4 => "WINDOW_BORDER_KCGRADIENT",
            _ => null
        };
    }

    private static string? GetOwnerDrawName(int value)
    {
        return value switch
        {
            0 => "None",
            0x0FA => "UI_OWNERDRAW_KEY_BIND_STATUS",
            >= 0x0FB and <= 0x109 => $"UI_OWNERDRAW_NOOP_{value:X3}",
            0x10A => "UI_OWNERDRAW_LOCAL_TALKING",
            0x10B => "UI_OWNERDRAW_TALKER_NUM_0",
            0x10C => "UI_OWNERDRAW_TALKER_NUM_1",
            0x10D => "UI_OWNERDRAW_TALKER_NUM_2",
            0x10E => "UI_OWNERDRAW_TALKER_NUM_3",
            0x10F => "UI_OWNERDRAW_NOOP_10F",
            0x110 => "UI_OWNERDRAW_LOGGED_IN_USER",
            0x111 => "UI_OWNERDRAW_RESERVED_SLOTS",
            0x112 => "UI_OWNERDRAW_NOOP_112",
            0x113 => "UI_OWNERDRAW_PLAYLIST_DESCRIPTION",
            0x114 => "UI_OWNERDRAW_LOGGED_IN_USER_NAME",
            0x115 => "UI_OWNERDRAW_NOOP_115",
            0x116 => "UI_OWNERDRAW_MAP_CUSTOM_DATA",
            _ => null
        };
    }

    private static string? GetTextStyleName(int value)
    {
        return value switch
        {
            0 => "ITEM_TEXTSTYLE_NORMAL",
            1 => "ITEM_TEXTSTYLE_BLINK",
            2 => "ITEM_TEXTSTYLE_PULSE",
            3 => "ITEM_TEXTSTYLE_SHADOWED",
            4 => "ITEM_TEXTSTYLE_OUTLINED",
            5 => "ITEM_TEXTSTYLE_OUTLINESHADOWED",
            6 => "ITEM_TEXTSTYLE_SHADOWEDMORE",
            _ => null
        };
    }

    private static string? GetTextAlignName(int value)
    {
        return value switch
        {
            0 => "ITEM_ALIGN_LEFT",
            1 => "ITEM_ALIGN_CENTER",
            2 => "ITEM_ALIGN_RIGHT",
            _ => null
        };
    }

    private static string ToDisplayName(string enumName, string prefix)
    {
        var name = enumName.StartsWith(prefix, StringComparison.Ordinal)
            ? enumName[prefix.Length..]
            : enumName;

        return string.Join(
            " ",
            name.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(ToTitleCase));
    }

    private static string ToTitleCase(string value)
    {
        return value.Length == 0
            ? value
            : string.Concat(
                value[..1].ToUpperInvariant(),
                value[1..].ToLowerInvariant());
    }
}
