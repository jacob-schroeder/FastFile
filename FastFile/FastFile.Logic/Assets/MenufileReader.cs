using FastFile.Logic.Extensions;
using FastFile.Models.Assets.TechniqueSet;

namespace FastFile.Logic.Assets;

public static class MenufileReader
{
    public static MaterialTechniqueSet Read(ReadOnlySpan<byte> span, ref int position)
    {
        var asset = new MaterialTechniqueSet()
        {
            Offset = position,
            NamePtr = Memory.ReadPointer<string>(span, ref position),
            WorldVertexFormat = (MaterialWorldVertexFormat)span.ReadInt32(ref position)
        };
        
        return asset;
    }
}
