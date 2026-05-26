using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public class XAssetList
{
    public int ScriptStringCount { get; set; }
    public ZonePointer<ZonePointer<string?>[]> ScriptStringsPtr { get; set; }
    public string?[] ScriptStrings { get; set; } = [];
    
    public int AssetCount { get; set; }
    public ZonePointer<XAsset[]> AssetsPtr { get; set; }
    public XAsset[] Assets { get; set; } = [];
}