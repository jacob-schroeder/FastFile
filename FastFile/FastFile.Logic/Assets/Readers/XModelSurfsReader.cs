using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class XModelSurfsReader
{
    public static XModelSurfs Read(ref ZoneReadContext context)
    {
        var asset = new XModelSurfs
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Surfs = context.ReadPointer<XSurface[]>(),
        };

        asset.NumSurfs = context.ReadUInt16();
        asset.PartBitsAlignment = context.ReadUInt16();
        for (var i = 0; i < asset.PartBits.Length; i++)
            asset.PartBits[i] = context.ReadInt32();

        return asset;
    }

    public static ZonePointer<XModelSurfs> ReadXModelSurfsPointer(ref ZoneReadContext context)
    {
        return context.ReadPointer<XModelSurfs>(
            (ref ZoneReadContext pointerContext, ZonePointer<XModelSurfs> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
    }
}
