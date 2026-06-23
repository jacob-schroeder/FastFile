using FastFile.Models.Database.Streaming;

namespace FastFile.Streaming;

public sealed class DbImageStreamEntryCursor
{
    private readonly IReadOnlyList<DbHeaderImageStreamEntry> _entries;
    private int _nextEntryIndex;

    public DbImageStreamEntryCursor(IReadOnlyList<DbHeaderImageStreamEntry> entries)
    {
        _entries = entries;
    }

    public int NextEntryIndex => _nextEntryIndex;
    public int Remaining => _entries.Count - _nextEntryIndex;

    public DbHeaderImageStreamEntry TakeNext()
    {
        if (_nextEntryIndex >= _entries.Count)
            throw new InvalidOperationException("DB image stream entry cursor ran past EntryCount.");

        return _entries[_nextEntryIndex++];
    }

    public DbHeaderImageStreamEntry[] TakeNextImageParts()
    {
        var parts = new DbHeaderImageStreamEntry[GfxImageStreamTable.StreamPartsPerImage];

        for (int i = 0; i < parts.Length; i++)
            parts[i] = TakeNext();

        return parts;
    }
}
