using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class RawFileReader
{
    public static RawFile Read(ref XFileReadContext context)
    {
        var asset = new RawFile
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            CompressedLen = context.ReadInt32(),
            Len = context.ReadInt32(),
        };

        asset.BufferPtr = context.ReadPointer<byte[]>(
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> pointer) =>
            {
                var length = asset.CompressedLen > 0 ? asset.CompressedLen : asset.Len + 1;
                byte[] value;
                pointerContext.PushStreamBlock(XFILE_BLOCK.LARGE);
                try
                {
                    value = pointerContext.ReadPointerValue(
                        pointer,
                        (ref XFileReadContext bufferContext) => bufferContext.ReadBytes(length));
                }
                finally
                {
                    pointerContext.PopStreamBlock();
                }

                pointer.SetResult(value);
            },
            PointerResolutionKind.Direct,
            "RawFile.Buffer");

        return asset;
    }
}
