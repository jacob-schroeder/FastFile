using FastFile.Logic.Extensions;
using FastFile.Models.Assets.TechniqueSet;

namespace FastFile.Logic.Assets;

public static class TechsetReader
{
    public static MaterialTechniqueSet Read(ReadOnlySpan<byte> span, ref int position)
    {
        var asset = new MaterialTechniqueSet()
        {
            Offset = position,
            NamePtr = Memory.ReadPointer<string>(span, ref position),
            WorldVertexFormat = (MaterialWorldVertexFormat)span.ReadInt32(ref position)
        };

        for (int i = 0; i < asset.Techniques.Length; i++)
        {
            asset.Techniques[i] = Memory.ReadPointer<int>(span, ref position);
        }

        Memory.ResolvePointer(asset.NamePtr, position);
        position = asset.NamePtr.Offset;
        
        asset.NamePtr.SetResult(span.ReadCStringAt(ref position));

        return asset;
    }
}
