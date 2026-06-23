using FastFile.Models.Database.Streaming;

namespace FastFile.Streaming.Images;

public sealed class ImageFileRegistry
{
    public const uint MaxImageFileIndex = 4;

    private readonly Dictionary<uint, StreamFileRef> _imageFiles = new();

    public ImageFileRegistry(StreamFileRef currentFastFile)
    {
        CurrentFastFile = currentFastFile;
    }

    public StreamFileRef CurrentFastFile { get; }

    public StreamFileRef? Resolve(DbHeaderImageStreamEntry entry)
    {
        if (entry.SourceEnd == 0)
            return null;

        if (entry.FileIndex == 0)
            return CurrentFastFile;

        if (entry.FileIndex > MaxImageFileIndex)
            throw new InvalidDataException($"Unexpected PS3 imagefile index {entry.FileIndex}.");

        return GetOrCreateImageFile(entry.FileIndex);
    }

    private StreamFileRef GetOrCreateImageFile(uint fileIndex)
    {
        if (_imageFiles.TryGetValue(fileIndex, out StreamFileRef existing))
            return existing;

        var created = new StreamFileRef(fileIndex, $"imagefile{fileIndex}", StreamFileKind.ImageFile);
        _imageFiles.Add(fileIndex, created);
        return created;
    }
}
