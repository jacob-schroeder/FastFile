using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.TechniqueSet;

namespace FastFile.Logic.Assets.Writers;

internal static class TechsetWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        var techset = (MaterialTechniqueSet)asset;
        GenericWriter.WriteStringPointer(context, techset.NamePtr);
        context.WriteByte((byte)techset.WorldVertexFormat);
        context.WriteByte(techset.HasBeenUploaded ? (byte)1 : (byte)0);
        context.WriteBytes(techset.Unused);

        foreach (var technique in techset.Techniques)
            context.WritePointer(technique);
    }
}
