using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class RawFileReader
{
    public static RawFile Read(ref ZoneReadContext context)
    {
        var asset = new RawFile
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            CompressedLen = context.ReadInt32(),
            Len = context.ReadInt32(),
        };

        asset.BufferPtr = context.ReadPointer<byte[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<byte[]> pointer) =>
            {
                var length = asset.CompressedLen > 0 ? asset.CompressedLen : asset.Len;
                var value = pointerContext.ReadPointerValue(
                    pointer,
                    (ref ZoneReadContext bufferContext) => bufferContext.ReadBytes(length));

                pointer.SetResult(value);
            });

        return asset;
    }
}
