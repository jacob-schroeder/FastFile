using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.XModels;

public class XModelSurfs() : BaseAsset(XAssetType.XModelSurfs)
{
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public DirectPointer<XSurface[]> Surfs { get; set; }
    public ushort NumSurfs { get; set; }
    public ushort PartBitsAlignment { get; set; }
    public int[] PartBits { get; set; } = new int[6];

    public override string? GetDisplayName => Name;
}
