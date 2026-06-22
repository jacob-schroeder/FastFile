using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Assets;

namespace FastFile.ModelsOLD.Zone;

public class XAsset
{
    public XAssetType Type { get; set; }
    
    
    //[XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public XPointer<BaseAsset> XAssetPtr { get; set; }
};
