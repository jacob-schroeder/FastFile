using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class XModelSurfsReader
{
    public static XModelSurfs Read(ref ZoneReadContext context)
    {
        var asset = new XModelSurfs
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
        };

        context.ReadPointer<byte>(); // XSurface* surfs
        asset.NumSurfs = context.ReadUInt16();
        context.ReadBytes(2); // align partBits
        context.ReadBytes(6 * 4);

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
