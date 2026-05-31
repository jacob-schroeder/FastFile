using FastFile.Models.Data;

namespace FastFile.Models.Assets.StringTables;

public class StringTableCell
{
    public ZonePointer<string> StringPtr { get; set; }
    public string String => StringPtr is { IsResolved: true } ? StringPtr.Result ?? string.Empty : string.Empty;

    public int Hash { get; set; }

    public static uint CalculateHash(string value)
    {
        uint hash = 0;

        foreach (var character in value)
            hash = (uint)(char.ToLowerInvariant(character) + (31 * hash));

        return hash;
    }
}
