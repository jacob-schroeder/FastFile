namespace FastFile.Logic.Database.Streaming;

public sealed class GfxImageStreamTable
{
    private readonly Dictionary<int, GfxImageStreamRecord[]> _recordsByImage = new();

    public void Set(int imageIndex, int partIndex, GfxImageStreamRecord record)
    {
        if ((uint)partIndex >= DbLoaderState.StreamPartsPerImage)
            throw new ArgumentOutOfRangeException(nameof(partIndex));

        if (!_recordsByImage.TryGetValue(imageIndex, out GfxImageStreamRecord[]? records))
        {
            records = new GfxImageStreamRecord[DbLoaderState.StreamPartsPerImage];
            _recordsByImage.Add(imageIndex, records);
        }

        records[partIndex] = record;
    }

    public GfxImageStreamRecord GetByStreamIndex(ushort streamIndex)
    {
        int imageIndex = streamIndex / DbLoaderState.StreamPartsPerImage;
        int partIndex = streamIndex & 3;

        if (!_recordsByImage.TryGetValue(imageIndex, out GfxImageStreamRecord[]? records))
            throw new KeyNotFoundException($"No GfxImage stream records have been created for image index {imageIndex}.");

        return records[partIndex];
    }
}
