using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Readers;

internal static class TechsetReader
{
    public static MaterialTechniqueSet Read(ref XFileReadContext context)
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
            asset.Techniques[i] = context.ReadDirectPointer<MaterialTechnique>($"Techset.Techniques[{i}]");

        return asset;
    }
}
