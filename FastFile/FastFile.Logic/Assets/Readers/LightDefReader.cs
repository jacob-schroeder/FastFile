using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Assets.Material;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class LightDefReader
{
    public static GfxLightDef Read(ref XFileReadContext context)
    {
        var asset = new GfxLightDef
        {
            Offset = context.Position,
            NamePtr = context.ReadDirectPointer<string>("GfxLightDef+0x00.Name"),
            AttenuationImage = context.ReadAliasPointer<GfxImage>("GfxLightDef+0x04.Attenuation.Image"),
            AttenuationSamplerState = context.ReadByte(),
            AttenuationPadding = context.ReadBytes(3),
            LmapLookupStart = context.ReadInt32(),
        };

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ImageReader.ResolveImagePointerNow(ref context, asset.AttenuationImage);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }
}
