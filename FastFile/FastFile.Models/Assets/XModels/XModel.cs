using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.XModels;

public class XModel() : BaseAsset(XAssetType.XModel)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public byte NumBones { get; set; }
    public byte NumRootBones { get; set; }
    public byte NumSurfs { get; set; }
    public byte NumLods { get; set; }

    public override string? GetDisplayName => Name;
}
