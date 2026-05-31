using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.XModels;

public class XModelSurfs() : BaseAsset(XAssetType.XModelSurfs)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public ushort NumSurfs { get; set; }

    public override string? GetDisplayName => Name;
}
