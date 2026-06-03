using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class RawFileWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        var rawFile = (RawFile)asset;
        GenericWriter.WriteStringPointer(context, rawFile.NamePtr);
        context.WriteInt32(rawFile.CompressedLen);
        context.WriteInt32(rawFile.Len);
        context.WritePointer(rawFile.BufferPtr, WriteBufferPointerValue);
    }

    private static void WriteBufferPointerValue(
        ZoneWriterContext context,
        ZonePointer<byte[]> pointer)
    {
        context.WriteBytes(pointer.Result);
    }
}
