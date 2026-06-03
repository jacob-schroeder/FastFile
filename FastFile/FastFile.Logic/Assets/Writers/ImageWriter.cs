using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class ImageWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteImage(context, (GfxImage)asset);
    }

    public static void WriteImagePointerValue(
        ZoneWriterContext context,
        ZonePointer<GfxImage> pointer)
    {
        if (pointer.Result is { } image)
            WriteImage(context, image);
    }

    private static void WriteImage(ZoneWriterContext context, GfxImage image)
    {
        context.WritePointer(image.LoadDef, WriteImageLoadDef);
        context.WriteByte(image.MapType);
        context.WriteByte(image.Semantic);
        context.WriteByte(image.Category);
        context.WriteByte(image.UseSrgbReads);
        context.WriteBytes(image.Picmip);
        context.WriteByte(image.NoPicmip);
        context.WriteByte(image.Track);
        foreach (var value in image.CardMemory)
            context.WriteInt32(value);
        context.WriteUInt16(image.Width);
        context.WriteUInt16(image.Height);
        context.WriteUInt16(image.Depth);
        context.WriteByte(image.DelayLoadPixels);
        context.WriteBytes(image.Pad);
        GenericWriter.WriteStringPointer(context, image.NamePtr);
    }

    private static void WriteImageLoadDef(
        ZoneWriterContext context,
        ZonePointer<GfxImageLoadDef> pointer)
    {
        if (pointer.Result is not { } loadDef)
            return;

        context.WriteByte(loadDef.LevelCount);
        context.WriteBytes(loadDef.Pad);
        context.WriteInt32(loadDef.Flags);
        context.WriteInt32(loadDef.Format);
        context.WriteInt32(loadDef.ResourceSize);
        context.WriteBytes(loadDef.Data);
    }
}
