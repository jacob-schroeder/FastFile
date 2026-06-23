using FastFile.Models.Database.Streaming;
using FastFile.Runtime;

namespace FastFile.Streaming.Images;

public sealed class GfxImageStreamingBinder
{
    public void Bind(
        GfxImageShell image,
        int imageIndex,
        DbImageStreamEntryCursor entries,
        ImageFileRegistry files,
        FastFileLoadContext context)
    {
        if (!image.HasStreamingData)
            return;

        for (int partIndex = 0; partIndex < GfxImageStreamTable.StreamPartsPerImage; partIndex++)
        {
            DbHeaderImageStreamEntry raw = entries.TakeNext();

            var record = new GfxImageStreamRecord(
                SourceStart: raw.SourceStart,
                BlockOffset: raw.BlockOffset,
                StreamOffset: raw.StreamOffset,
                SourceEnd: raw.SourceEnd,
                File: files.Resolve(raw));

            context.ImageStreams.Set(imageIndex, partIndex, record);
        }
    }
}
