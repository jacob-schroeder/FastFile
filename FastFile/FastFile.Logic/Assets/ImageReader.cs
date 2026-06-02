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

        asset.LoadDef = context.ReadPointer<GfxImageLoadDef>(ReadImageLoadDef);
        asset.MapType = context.ReadByte();
        asset.Semantic = context.ReadByte();
        asset.Category = context.ReadByte();
        asset.UseSrgbReads = context.ReadByte();
        asset.Picmip = context.ReadBytes(2);
        asset.NoPicmip = context.ReadByte();
        asset.Track = context.ReadByte();
        asset.CardMemory =
        [
            context.ReadInt32(),
            context.ReadInt32()
        ];
        asset.Width = context.ReadUInt16();
        asset.Height = context.ReadUInt16();
        asset.Depth = context.ReadUInt16();
        asset.DelayLoadPixels = context.ReadByte();
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

    private static GfxImageLoadDef ReadImageLoadDef(ref ZoneReadContext context)
    {
        var loadDef = new GfxImageLoadDef
        {
            LevelCount = context.ReadByte(),
            Pad = context.ReadBytes(3),
            Flags = context.ReadInt32(),
            Format = context.ReadInt32(),
            ResourceSize = context.ReadInt32()
        };

        if (loadDef.ResourceSize > 0)
        {
            loadDef.Data = context.ReadBytes(loadDef.ResourceSize);
        }

        return loadDef;
    }
}
