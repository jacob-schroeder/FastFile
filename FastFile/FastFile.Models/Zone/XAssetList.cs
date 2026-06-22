using FastFile.Models.Pointers;

namespace FastFile.Models.Zone;

public struct XAssetList
{
    public int ScriptStringCount;
    public XArray<XString> ScriptStrings;
    public int AssetCount;
    public XArray<XAsset> Assets;
}