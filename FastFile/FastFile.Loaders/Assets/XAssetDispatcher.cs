using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.Menu;
using FastFile.Loaders.Assets.RawFile;
using FastFile.Loaders.Assets.StringTable;
using FastFile.Loaders.Assets.StructuredData;
using FastFile.Loaders.Assets.TechniqueSet;
using FastFile.Models.Assets;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets;

public sealed class XAssetDispatcher
{
    private readonly MenuFileLoader _menuFileLoader = new();
    private readonly MaterialLoader _materialLoader = new();
    private readonly MaterialTechniqueSetLoader _techsetLoader = new();
    private readonly StringTableLoader _stringTableLoader = new();
    private readonly StructuredDataDefSetLoader _structuredDataDefSetLoader = new();
    private readonly RawFileLoader _rawFileLoader = new();

    public IReadOnlyList<XAssetLoadResult> LoadSupportedPrefix(
        FastFileCursor cursor,
        XAssetListSnapshot assetList,
        FastFileLoadContext context)
    {
        var results = new List<XAssetLoadResult>();

        foreach (XAssetEntry asset in assetList.Assets)
        {
            context.Diagnostics.Trace(
                $"asset[{asset.Index}] table=0x{asset.SerializedOffset:X} type={asset.Type} ptr={asset.AssetPointer} begin source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");

            if (asset.AssetPointer.Type == PointerType.Null)
            {
                context.Diagnostics.Trace(
                    $"asset[{asset.Index}] type={asset.Type} null pointer end source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
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
                asset.Type != XAssetType.Material &&
                asset.Type != XAssetType.MenuFile &&
                asset.Type != XAssetType.StringTable &&
                asset.Type != XAssetType.StructuredDataDef &&
                asset.Type != XAssetType.RawFile)
            {
                context.Diagnostics.Trace(
                    $"asset[{asset.Index}] type={asset.Type} unsupported end source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
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

            try
            {
                PatchInlineAssetPointer(asset, context);

                if (asset.Type == XAssetType.Techset)
                {
                    loadedAsset = _techsetLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Material)
                {
                    loadedAsset = _materialLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.MenuFile)
                {
                    loadedAsset = _menuFileLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context, out stopReason);
                }
                else if (asset.Type == XAssetType.StringTable)
                {
                    loadedAsset = _stringTableLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.StructuredDataDef)
                {
                    loadedAsset = _structuredDataDefSetLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else
                {
                    loadedAsset = _rawFileLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
            }
            catch (Exception ex)
            {
                context.Diagnostics.Trace(
                    $"asset[{asset.Index}] type={asset.Type} FAILED source=0x{sourceOffset:X} cursor=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()} exception={ex.GetType().Name}: {ex.Message}");
                throw;
            }

            context.Diagnostics.Trace(
                $"asset[{asset.Index}] type={asset.Type} loaded source=0x{sourceOffset:X}..0x{cursor.Offset:X} assetRoot=0x{loadedAsset.Offset:X} stop={stopReason ?? "<none>"} blocks={context.Blocks.DescribePositions()}");

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

    private static void PatchInlineAssetPointer(
        XAssetEntry asset,
        FastFileLoadContext context)
    {
        if (asset.AssetPointer.Type is not (PointerType.Inline or PointerType.Insert))
            return;

        XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(
            asset.AssetPointerCellAddress,
            asset.AssetPointer.Raw,
            alignment: 4);
        int runtimePointer = XPointerCodec.Encode(targetAddress);
        context.Diagnostics.Trace(
            $"asset[{asset.Index}] patched asset cell {asset.AssetPointerCellAddress}=0x{runtimePointer:X8} for {asset.Type} root");
    }
}
