namespace FastFile.Logic.Database.Streaming;

public sealed class DbLoaderState
{
    public const int StreamPartsPerImage = 4;
    public const uint MaxImageFileIndex = 4;

    private DbHeaderImageStreamEntry[] _entries = [];
    private int _nextEntryIndex;
    private readonly Dictionary<uint, StreamFileRef> _imageFiles = new();

    public uint SelectedLanguageMask { get; set; }

    public StreamFileRef CurrentFastFile { get; set; } = new(0, "<current fastfile>", StreamFileKind.CurrentFastFile);

    public GfxImageStreamTable ImageStreams { get; } = new();

    public void ResetHeaderEntries(DbHeaderImageStreamEntry[] entries)
    {
        _entries = entries;
        _nextEntryIndex = 0;
    }

    public DbHeaderImageStreamEntry TakeNextHeaderEntry()
    {
        if ((uint)_nextEntryIndex >= (uint)_entries.Length)
            throw new InvalidOperationException("DB image stream entry cursor ran past EntryCount.");

        return _entries[_nextEntryIndex++];
    }

    public StreamFileRef GetOrCreateImageFile(uint fileIndex)
    {
        if (_imageFiles.TryGetValue(fileIndex, out StreamFileRef existing))
            return existing;

        var created = new StreamFileRef(fileIndex, $"imagefile{fileIndex}", StreamFileKind.ImageFile);
        _imageFiles.Add(fileIndex, created);
        return created;
    }
}
