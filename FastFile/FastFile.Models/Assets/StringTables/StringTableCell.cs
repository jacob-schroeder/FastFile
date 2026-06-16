using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.StringTables;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
[XEbootEvidence(
    "0x1039d8",
    "eboot/traces/xasset_loader_findings.txt",
    Detail = "StringTableCell inner loader: Load_Stream size 0x08; Load_XString at cell+0x00; hash at +0x04 is copied by the root stream and not otherwise transformed.")]
public class StringTableCell
{
    private string? _logicalStringOverride;

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString,
        PayloadBlock = XFILE_BLOCK.LARGE)]
    public XPointer<string> StringPtr { get; set; } // Direct
    public string PointerString => StringPtr is { IsResolved: true } ? StringPtr.Value ?? string.Empty : string.Empty;
    public string String => _logicalStringOverride ?? PointerString;
    public bool HasLogicalStringOverride => _logicalStringOverride is not null;

    [XField(Offset = 0x04)]
    public int Hash { get; set; }

    public void SetLogicalStringOverride(string value)
    {
        _logicalStringOverride = value;
    }

    public void SetString(string value)
    {
        _logicalStringOverride = null;
        //I think this is incorrect
        //StringPtr.SetRaw(-1);
        StringPtr.Value = value;
        Hash = unchecked((int)CalculateHash(value));
    }

    public static void ApplyLogicalStringValues(IReadOnlyList<StringTableCell> cells)
    {
        var canonicalByHash = new Dictionary<uint, string>();
        foreach (var cell in cells)
        {
            var value = cell.PointerString;
            if (string.IsNullOrEmpty(value))
                continue;

            var expectedHash = unchecked((uint)cell.Hash);
            if (CalculateHash(value) == expectedHash)
                canonicalByHash.TryAdd(expectedHash, value);
        }

        if (canonicalByHash.Count == 0)
            return;

        foreach (var cell in cells)
        {
            var value = cell.PointerString;
            if (string.IsNullOrEmpty(value))
                continue;

            var expectedHash = unchecked((uint)cell.Hash);
            if (CalculateHash(value) == expectedHash)
                continue;

            if (canonicalByHash.TryGetValue(expectedHash, out var logicalValue))
                cell.SetLogicalStringOverride(logicalValue);
        }
    }

    public static uint CalculateHash(string value)
    {
        uint hash = 0;

        foreach (var character in value)
            hash = (uint)(char.ToLowerInvariant(character) + (31 * hash));

        return hash;
    }
}
