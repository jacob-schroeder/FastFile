using Avalonia.Controls;
using FastFile.Models.Assets.StructuredData;
using UI.Models;
using System.Globalization;
using System.Linq;

namespace UI.Views.Assets;

public partial class StructuredDataAssetView : UserControl
{
    public StructuredDataAssetView()
    {
        InitializeComponent();
    }

    public StructuredDataAssetView(StructuredDataDefSet structuredDataDefSet) : this()
    {
        var displayName = GetDisplayName(structuredDataDefSet);
        StructuredDataNameTextBlock.Text = displayName;
        StructuredDataSummaryTextBlock.Text = $"Structured data definition set · {structuredDataDefSet.DefCount:N0} declared defs";

        var defs = BuildDefinitions(structuredDataDefSet).ToArray();
        StructuredDataDefCountTextBlock.Text = $"{defs.Length:N0} loaded definitions";
        StructuredDataSummaryItemsControl.ItemsSource = BuildSummaryItems(structuredDataDefSet, defs.Length);
        StructuredDataDefsItemsControl.ItemsSource = defs;
        StructuredDataDefsEmptyTextBlock.IsVisible = defs.Length == 0;
    }

    private static string GetDisplayName(StructuredDataDefSet structuredDataDefSet)
    {
        return string.IsNullOrWhiteSpace(structuredDataDefSet.GetDisplayName)
            ? "(unnamed structured data set)"
            : structuredDataDefSet.GetDisplayName;
    }

    private static KeyValueListItem[] BuildSummaryItems(StructuredDataDefSet structuredDataDefSet, int resolvedDefCount)
    {
        return
        [
            new("Display Name", GetDisplayName(structuredDataDefSet)),
            new("Offset", $"0x{structuredDataDefSet.Offset:X8}"),
            new("Name Pointer", AssetViewFormatters.FormatPointerRaw(structuredDataDefSet.NamePtr)),
            new("Declared Def Count", $"{structuredDataDefSet.DefCount:N0}"),
            new("Loaded Def Count", $"{resolvedDefCount:N0}")
        ];
    }

    private static StructuredDataDefDisplayItem[] BuildDefinitions(StructuredDataDefSet structuredDataDefSet)
    {
        return structuredDataDefSet.Defs
            .Select((def, index) => BuildDefinitionItem(def, index))
            .ToArray();
    }

    private static StructuredDataDefDisplayItem BuildDefinitionItem(StructuredDataDef definition, int index)
    {
        var enumLines = BuildEnumLines(definition);
        var structLines = BuildStructLines(definition);
        var indexedArrayLines = BuildIndexedArrayLines(definition);
        var enumedArrayLines = BuildEnumedArrayLines(definition);

        return new StructuredDataDefDisplayItem(
            name: $"Definition {index + 1}",
            version: definition.Version.ToString(CultureInfo.CurrentCulture),
            formatChecksum: $"{definition.FormatChecksum:X8}",
            rootType: FormatType(definition.RootType),
            size: $"{definition.Size:N0}",
            enumCountText: $"declared {definition.EnumCount:N0}, resolved {definition.Enums.Length:N0}",
            structCountText: $"declared {definition.StructCount:N0}, resolved {definition.Structs.Length:N0}",
            indexedArrayCountText: $"declared {definition.IndexedArrayCount:N0}, resolved {definition.IndexedArrays.Length:N0}",
            enumedArrayCountText: $"declared {definition.EnumedArrayCount:N0}, resolved {definition.EnumedArrays.Length:N0}",
            enumSectionTitle: $"Enums ({definition.Enums.Length:N0} / {definition.EnumCount:N0})",
            structSectionTitle: $"Structs ({definition.Structs.Length:N0} / {definition.StructCount:N0})",
            indexedArraySectionTitle: $"Indexed Arrays ({definition.IndexedArrays.Length:N0} / {definition.IndexedArrayCount:N0})",
            enumedArraySectionTitle: $"Enumed Arrays ({definition.EnumedArrays.Length:N0} / {definition.EnumedArrayCount:N0})",
            enumLines: enumLines,
            structLines: structLines,
            indexedArrayLines: indexedArrayLines,
            enumedArrayLines: enumedArrayLines);
    }

    private static string[] BuildEnumLines(StructuredDataDef definition)
    {
        if (definition.Enums.Length == 0)
        {
            return [definition.EnumCount == 0
                ? "No enums declared."
                : "Enums are declared but this set has not loaded entries yet."];
        }

        var lines = definition.Enums
            .Select((enumValue, index) =>
                BuildEnumLines(enumValue, index))
            .SelectMany(lines => lines);

        return lines.ToArray();
    }

    private static string[] BuildEnumLines(StructuredDataEnum enumValue, int enumIndex)
    {
        var headerLines = new[]
        {
            $"[{enumIndex + 1}] entries: declared {enumValue.EntryCount:N0}, reserved {enumValue.ReservedEntryCount:N0}, resolved {enumValue.Entries.Length:N0}",
            $"     pointer {AssetViewFormatters.FormatPointerRaw(enumValue.EntriesPtr)}"
        };

        var entriesLines = enumValue.Entries.Length == 0
            ? [enumValue.EntryCount == 0
                ? "     no entries."
                : "     entries are unavailable."]
            : enumValue.Entries.Select(entry =>
                $"     - #{entry.Index:D4} '{(string.IsNullOrWhiteSpace(entry.String) ? "(unnamed)" : entry.String)}'");

        return [.. headerLines, .. entriesLines];
    }

    private static string[] BuildStructLines(StructuredDataDef definition)
    {
        if (definition.Structs.Length == 0)
        {
            return [definition.StructCount == 0
                ? "No structs declared."
                : "Structs are declared but this set has not loaded fields yet."];
        }

        var lines = definition.Structs
            .SelectMany((structValue, index) => BuildStructLines(structValue, index));

        return lines.ToArray();
    }

    private static string[] BuildStructLines(StructuredDataStruct structValue, int structIndex)
    {
        var properties = structValue.Properties.Length == 0
            ? [structValue.PropertyCount == 0
                ? "     no properties."
                : "     properties are unavailable."]
            : structValue.Properties.Select(property =>
                $"     - {GetStructPropertyName(property)} | type {FormatType(property.Type)} | offset 0x{property.Offset:X8}");

        return
        [
            $"[{structIndex + 1}] properties: declared {structValue.PropertyCount:N0}, resolved {structValue.Properties.Length:N0}, size 0x{structValue.Size:X8}, bitOffset 0x{structValue.BitOffset:X8}",
            $"     pointer {AssetViewFormatters.FormatPointerRaw(structValue.PropertiesPtr)}",
            .. properties
        ];
    }

    private static string[] BuildIndexedArrayLines(StructuredDataDef definition)
    {
        if (definition.IndexedArrays.Length == 0)
        {
            return [definition.IndexedArrayCount == 0
                ? "No indexed arrays declared."
                : "Indexed arrays are declared but this set has not loaded descriptors yet."];
        }

        return definition.IndexedArrays
            .Select((arrayValue, index) =>
                $"[{index + 1}] size {arrayValue.ArraySize:N0}, element size {arrayValue.ElementSize:N0}, element type {FormatType(arrayValue.ElementType)}")
            .ToArray();
    }

    private static string[] BuildEnumedArrayLines(StructuredDataDef definition)
    {
        if (definition.EnumedArrays.Length == 0)
        {
            return [definition.EnumedArrayCount == 0
                ? "No enumed arrays declared."
                : "Enumed arrays are declared but this set has not loaded descriptors yet."];
        }

        return definition.EnumedArrays
            .Select((arrayValue, index) =>
                $"[{index + 1}] enum index {arrayValue.EnumIndex:N0}, element size {arrayValue.ElementSize:N0}, element type {FormatType(arrayValue.ElementType)}")
            .ToArray();
    }

    private static string GetStructPropertyName(StructuredDataStructProperty property)
    {
        return string.IsNullOrWhiteSpace(property.Name)
            ? "(unnamed)"
            : property.Name;
    }

    private static string FormatType(StructuredDataType type)
    {
        return $"{type.Type} ({type.UnionValue:N0})";
    }
}
