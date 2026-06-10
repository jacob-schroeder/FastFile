using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public class XAssetListOLD
{
    public int ScriptStringCount { get; set; }
    public DirectPointer<ZonePointer<string?>[]> ScriptStringsPtr { get; set; }
    public string?[] ScriptStrings => ScriptStringsPtr is { IsResolved: true, Result: not null }
        ? ScriptStringsPtr.Result.Select(pointer => pointer.Result).ToArray()
        : Array.Empty<string?>();
    
    public int AssetCount { get; set; }
    public DirectPointer<XAsset[]> AssetsPtr { get; set; }
    public XAsset[] Assets => AssetsPtr is { IsResolved: true, Result: not null }
        ? AssetsPtr.Result
        : Array.Empty<XAsset>();
}
