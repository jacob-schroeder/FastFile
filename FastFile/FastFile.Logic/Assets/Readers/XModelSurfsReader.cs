using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class XModelSurfsReader
{
    public static XModelSurfs Read(ref XFileReadContext context)
    {
        var asset = new XModelSurfs
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            Surfs = context.ReadDirectPointer<XSurface[]>("XModelSurfs.Surfs"),
        };

        asset.NumSurfs = context.ReadUInt16();
        asset.PartBitsAlignment = context.ReadUInt16();
        for (var i = 0; i < asset.PartBits.Length; i++)
            asset.PartBits[i] = context.ReadInt32();

        return asset;
    }

    public static ZonePointer<XModelSurfs> ReadXModelSurfsPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<XModelSurfs>("XModelSurfsAssetRef");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<XModelSurfs> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
        return pointer;
    }
}
