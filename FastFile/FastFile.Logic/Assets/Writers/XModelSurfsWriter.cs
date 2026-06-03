using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class XModelSurfsWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteXModelSurfsValue(context, (XModelSurfs)asset);
    }

    public static void WriteXModelSurfsPointer(ZoneWriterContext context, ZonePointer<XModelSurfs>? pointer)
    {
        context.WritePointer(pointer, WriteXModelSurfsPointerValue);
    }

    private static void WriteXModelSurfsPointerValue(ZoneWriterContext context, ZonePointer<XModelSurfs> pointer)
    {
        if (pointer.Result is { } value)
            WriteXModelSurfsValue(context, value);
    }

    private static void WriteXModelSurfsValue(ZoneWriterContext context, XModelSurfs asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WritePointerRaw(asset.Surfs);
        context.WriteUInt16(asset.NumSurfs);
        context.WriteUInt16(asset.PartBitsAlignment);
        foreach (var partBits in asset.PartBits)
            context.WriteInt32(partBits);
    }
}
