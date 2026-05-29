using FastFile.Models.Zone;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.TechniqueSet;

public class MaterialTechniqueSet() : BaseAsset(XAssetType.Techset)
{
    #if PS3
    private const int MAX_TECHNIQUES = 37;
    #elif XBOX
    private const int MAX_TECHNIQUES = 33;
    #elif PC
    private const int MAX_TECHNIQUES = 48;
    #endif
    
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result : string.Empty;
    
    public MaterialWorldVertexFormat WorldVertexFormat { get; set; }

    //This is not int, just placeholder for the first asset in my test.ff
    public ZonePointer<int>[] Techniques { get; set; } = new ZonePointer<int>[MAX_TECHNIQUES];

    public override string? GetDisplayName => Name;
}
