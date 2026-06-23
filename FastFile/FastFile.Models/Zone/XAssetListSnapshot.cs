using FastFile.Models.Assets;
using FastFile.Models.Pointers;

namespace FastFile.Models.Zone;

public sealed record XAssetListSnapshot(
    int SerializedOffset,
    int ScriptStringCount,
    XPointer<XString[]> ScriptStringsPointer,
    IReadOnlyList<XScriptStringEntry> ScriptStrings,
    int AssetCount,
    XPointer<XAssetEntry[]> AssetsPointer,
    IReadOnlyList<XAssetEntry> Assets);

public sealed record XScriptStringEntry(
    int Index,
    int PointerSerializedOffset,
    XString Pointer,
    string? Value);

public sealed record XAssetEntry(
    int Index,
    int SerializedOffset,
    XAssetType Type,
    XPointer<BaseAsset> AssetPointer);
