using FastFile.Models.Data;

namespace FastFile.Models.Zone;

public class XAssetList
{
    public int ScriptStringCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(ScriptStringCount))]
    public DirectPointer<ZonePointer<string?>[]> ScriptStringsPtr { get; set; }
    public string?[] ScriptStrings => ScriptStringsPtr is { IsResolved: true, Result: not null }
        ? ScriptStringsPtr.Result.Select(pointer => pointer.Result).ToArray()
        : Array.Empty<string?>();
    
    public int AssetCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(AssetCount))]
    public DirectPointer<XAsset[]> AssetsPtr { get; set; }
    public XAsset[] Assets => AssetsPtr is { IsResolved: true, Result: not null }
        ? AssetsPtr.Result
        : Array.Empty<XAsset>();
}
