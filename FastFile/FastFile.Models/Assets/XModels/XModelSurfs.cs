using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.XModels;

public class XModelSurfs() : BaseAsset(XAssetType.XModelSurfs)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public XPointer<XSurface[]> Surfs { get; set; } // Direct
    public ushort NumSurfs { get; set; }
    public ushort PartBitsAlignment { get; set; }
    public int[] PartBits { get; set; } = new int[6];

    public override string? GetDisplayName => Name;
}
