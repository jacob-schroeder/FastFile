using FastFile.Models.Data;

namespace FastFile.Models.Assets.StringTables;

public class StringTableCell
{
    private string? _logicalStringOverride;

    public ZonePointer<string> StringPtr { get; set; }
    public string PointerString => StringPtr is { IsResolved: true } ? StringPtr.Result ?? string.Empty : string.Empty;
    public string String => _logicalStringOverride ?? PointerString;
    public bool HasLogicalStringOverride => _logicalStringOverride is not null;

    public int Hash { get; set; }

    public void SetLogicalStringOverride(string value)
    {
        _logicalStringOverride = value;
    }

    public void SetString(string value)
    {
        _logicalStringOverride = null;
        StringPtr.SetRaw(-1);
        StringPtr.SetResult(value);
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
