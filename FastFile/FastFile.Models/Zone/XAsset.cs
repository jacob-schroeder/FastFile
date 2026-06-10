using FastFile.Models.Assets;
using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public class XAsset
{
    public XAssetType Type { get; set; }
    
    
    //[XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public XPointer<BaseAsset> XAssetPtr { get; set; }
};
