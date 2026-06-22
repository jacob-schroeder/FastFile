using FastFile.ModelsOLD.Data;

namespace FastFile.ModelsOLD.Zone;

public class XAssetList
{
    public int ScriptStringCount { get; set; }
    public XPointer<XPointer<string?>[]> ScriptStringsPtr { get; set; }
    public string?[] ScriptStrings => ScriptStringsPtr is { IsResolved: true, Value: not null }
        ? ScriptStringsPtr.Value.Select(pointer => pointer.Value).ToArray()
        : Array.Empty<string?>();
    
    public int AssetCount { get; set; }
    public XPointer<XAsset[]> AssetsPtr { get; set; }
    public XAsset[] Assets => AssetsPtr is { IsResolved: true, Value: not null }
        ? AssetsPtr.Value
        : Array.Empty<XAsset>();
}