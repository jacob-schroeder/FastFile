using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets;

internal delegate BaseAsset XAssetReader(ReadOnlySpan<byte> span, ref int position);

internal static class XAssetReaderRegistry
{
    private static readonly IReadOnlyDictionary<XAssetType, XAssetReader> Readers =
        new Dictionary<XAssetType, XAssetReader>
        {
            [XAssetType.Techset] = TechsetReader.Read,
            [XAssetType.MenuFile] = MenufileReader.Read,
            // tbd...
        };

    public static bool TryGetReader(XAssetType type, out XAssetReader reader)
    {
        return Readers.TryGetValue(type, out reader!);
    }
}
