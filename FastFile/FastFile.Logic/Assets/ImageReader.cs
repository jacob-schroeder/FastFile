using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class ImageReader
{
    public static GfxImage Read(ref ZoneReadContext context)
    {
        var asset = new GfxImage
        {
            Offset = context.Position,
        };

        context.ReadPointer<byte>(); // texture.loadDef
        context.ReadBytes(4); // mapType, semantic, category, useSrgbReads
        context.ReadBytes(2); // picmip
        context.ReadByte(); // noPicmip
        context.ReadByte(); // track
        context.ReadBytes(8); // cardMemory
        asset.Width = context.ReadUInt16();
        asset.Height = context.ReadUInt16();
        asset.Depth = context.ReadUInt16();
        context.ReadByte(); // delayLoadPixels
        context.ReadBytes(3); // pad before pointer
        asset.NamePtr = GenericReader.ReadStringPointer(ref context);

        return asset;
    }

    public static ZonePointer<GfxImage> ReadImagePointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<GfxImage>(
            (ref ZoneReadContext pointerContext, ZonePointer<GfxImage> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, Read);
                pointer.SetResult(value);
            });
    }
}
