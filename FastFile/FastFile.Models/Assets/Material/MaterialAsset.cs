using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Material;

public sealed class MaterialAsset : BaseAsset
{
    public const int SerializedSize = 0xa8;

    public XAssetType Type => XAssetType.Material;
}
