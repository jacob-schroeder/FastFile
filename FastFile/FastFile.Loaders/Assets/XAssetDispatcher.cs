using FastFile.Loaders.Assets.Menu;
using FastFile.Loaders.Assets.TechniqueSet;
using FastFile.Models.Assets;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets;

public sealed class XAssetDispatcher
{
    private readonly MenuFileLoader _menuFileLoader = new();
    private readonly MaterialTechniqueSetLoader _techsetLoader = new();

    public IReadOnlyList<XAssetLoadResult> LoadSupportedPrefix(
        FastFileCursor cursor,
        XAssetListSnapshot assetList,
        FastFileLoadContext context)
    {
        var results = new List<XAssetLoadResult>();

        foreach (XAssetEntry asset in assetList.Assets)
        {
            if (asset.AssetPointer.Type == PointerType.Null)
            {
                results.Add(new XAssetLoadResult(
                    asset.Index,
                    asset.Type,
                    cursor.Offset,
                    cursor.Offset,
                    asset.AssetPointer,
                    null,
                    "null asset pointer"));
                continue;
            }

            if (asset.Type != XAssetType.Techset &&
                asset.Type != XAssetType.MenuFile)
            {
                results.Add(new XAssetLoadResult(
                    asset.Index,
                    asset.Type,
                    cursor.Offset,
                    cursor.Offset,
                    asset.AssetPointer,
                    null,
                    $"unsupported asset type {asset.Type}"));
                break;
            }

            int sourceOffset = cursor.Offset;
            BaseAsset loadedAsset;
            string? stopReason = null;

            if (asset.Type == XAssetType.Techset)
            {
                loadedAsset = _techsetLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
            }
            else
            {
                loadedAsset = _menuFileLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context, out stopReason);
            }

            results.Add(new XAssetLoadResult(
                asset.Index,
                asset.Type,
                sourceOffset,
                cursor.Offset,
                asset.AssetPointer,
                loadedAsset,
                stopReason));

            if (stopReason is not null)
                break;
        }

        return results;
    }
}
