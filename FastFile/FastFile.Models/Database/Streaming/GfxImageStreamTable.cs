namespace FastFile.Models.Database.Streaming;

public sealed class GfxImageStreamTable
{
    public const int StreamPartsPerImage = 4;
    public const int RuntimeStreamPartStride = 0xE00;

    private readonly Dictionary<int, GfxImageStreamRecord[]> _recordsByImage = new();

    public void Set(int imageIndex, int partIndex, GfxImageStreamRecord record)
    {
        if ((uint)partIndex >= StreamPartsPerImage)
            throw new ArgumentOutOfRangeException(nameof(partIndex));

        if (!_recordsByImage.TryGetValue(imageIndex, out GfxImageStreamRecord[]? records))
        {
            records = new GfxImageStreamRecord[StreamPartsPerImage];
            _recordsByImage.Add(imageIndex, records);
        }

        records[partIndex] = record;
    }

    public GfxImageStreamRecord GetByStreamIndex(ushort streamIndex)
    {
        int partIndex = streamIndex / RuntimeStreamPartStride;
        int imageIndex = streamIndex - partIndex * RuntimeStreamPartStride;

        if ((uint)partIndex >= StreamPartsPerImage)
            throw new ArgumentOutOfRangeException(nameof(streamIndex), $"Runtime GfxImage stream index 0x{streamIndex:X} resolves to invalid part {partIndex}.");

        if (!_recordsByImage.TryGetValue(imageIndex, out GfxImageStreamRecord[]? records))
            throw new KeyNotFoundException($"No GfxImage stream records have been created for image index {imageIndex}.");

        return records[partIndex];
    }
}
