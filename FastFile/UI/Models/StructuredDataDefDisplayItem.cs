namespace UI.Models;

public sealed class StructuredDataDefDisplayItem(
    string name,
    string version,
    string formatChecksum,
    string rootType,
    string size,
    string enumCountText,
    string structCountText,
    string indexedArrayCountText,
    string enumedArrayCountText,
    string enumSectionTitle,
    string structSectionTitle,
    string indexedArraySectionTitle,
    string enumedArraySectionTitle,
    string[] enumLines,
    string[] structLines,
    string[] indexedArrayLines,
    string[] enumedArrayLines)
{
    public string Name { get; } = name;

    public string Version { get; } = version;

    public string FormatChecksum { get; } = formatChecksum;

    public string RootType { get; } = rootType;

    public string Size { get; } = size;

    public string EnumCountText { get; } = enumCountText;

    public string StructCountText { get; } = structCountText;

    public string IndexedArrayCountText { get; } = indexedArrayCountText;

    public string EnumedArrayCountText { get; } = enumedArrayCountText;

    public string EnumSectionTitle { get; } = enumSectionTitle;

    public string StructSectionTitle { get; } = structSectionTitle;

    public string IndexedArraySectionTitle { get; } = indexedArraySectionTitle;

    public string EnumedArraySectionTitle { get; } = enumedArraySectionTitle;

    public string[] EnumLines { get; } = enumLines;

    public string[] StructLines { get; } = structLines;

    public string[] IndexedArrayLines { get; } = indexedArrayLines;

    public string[] EnumedArrayLines { get; } = enumedArrayLines;

    public string VersionAndChecksum => $"{Version} · checksum 0x{FormatChecksum}";
}
