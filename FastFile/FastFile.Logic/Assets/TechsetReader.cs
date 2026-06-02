using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.TechniqueSet;

namespace FastFile.Logic.Assets;

internal static class TechsetReader
{
    public static MaterialTechniqueSet Read(ref ZoneReadContext context)
    {
        var asset = new MaterialTechniqueSet
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context),
            WorldVertexFormat = (MaterialWorldVertexFormat)context.ReadByte(),
            HasBeenUploaded = context.ReadByte() != 0,
            Unused = context.ReadBytes(2),
        };

        for (var i = 0; i < asset.Techniques.Length; i++)
            asset.Techniques[i] = context.ReadPointer<MaterialTechnique>();

        return asset;
    }
}
