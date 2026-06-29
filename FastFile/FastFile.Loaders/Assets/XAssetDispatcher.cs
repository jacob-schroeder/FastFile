using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.Menu;
using FastFile.Loaders.Assets.Font;
using FastFile.Loaders.Assets.Fx;
using FastFile.Loaders.Assets.ImpactFx;
using FastFile.Loaders.Assets.LightDef;
using FastFile.Loaders.Assets.Localize;
using FastFile.Loaders.Assets.Physics;
using FastFile.Loaders.Assets.RawFile;
using FastFile.Loaders.Assets.Sound;
using FastFile.Loaders.Assets.StringTable;
using FastFile.Loaders.Assets.StructuredData;
using FastFile.Loaders.Assets.TechniqueSet;
using FastFile.Loaders.Assets.Vehicle;
using FastFile.Loaders.Assets.Weapon;
using FastFile.Loaders.Assets.XAnim;
using FastFile.Loaders.Assets.XModel;
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
    private readonly FontLoader _fontLoader = new();
    private readonly MaterialTechniqueSetLoader _techsetLoader = new();
    private readonly StringTableLoader _stringTableLoader = new();
    private readonly StructuredDataDefSetLoader _structuredDataDefSetLoader = new();
    private readonly RawFileLoader _rawFileLoader = new();
    private readonly LocalizeLoader _localizeLoader = new();
    private readonly WeaponLoader _weaponLoader = new();
    private readonly SoundAliasListLoader _soundLoader = new();
    private readonly FxEffectDefLoader _fxLoader = new();
    private readonly FxImpactTableLoader _impactFxLoader = new();
    private readonly XAnimPartsLoader _xanimLoader = new();
    private readonly XModelLoader _xmodelLoader = new();
    private readonly PhysCollmapLoader _physCollmapLoader = new();
    private readonly VehicleDefLoader _vehicleLoader = new();
    private readonly LightDefLoader _lightDefLoader = new();

    public IReadOnlyList<XAssetLoadResult> LoadSupportedPrefix(
        FastFileCursor cursor,
        XAssetListSnapshot assetList,
        FastFileLoadContext context)
    {
        var results = new List<XAssetLoadResult>();

        foreach (XAssetEntry asset in assetList.Assets)
        {
            using IDisposable assetCoverageScope = context.SourceCoverage.PushOwner($"asset[{asset.Index}] {asset.Type}");
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
                asset.Type != XAssetType.RawFile &&
                asset.Type != XAssetType.Localize &&
                asset.Type != XAssetType.Sound &&
                asset.Type != XAssetType.Fx &&
                asset.Type != XAssetType.ImpactFx &&
                asset.Type != XAssetType.XAnim &&
                asset.Type != XAssetType.XModel &&
                asset.Type != XAssetType.PhysCollmap &&
                asset.Type != XAssetType.Font &&
                asset.Type != XAssetType.Vehicle &&
                asset.Type != XAssetType.LightDef &&
                asset.Type != XAssetType.Weapon)
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
                else if (asset.Type == XAssetType.RawFile)
                {
                    loadedAsset = _rawFileLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Localize)
                {
                    loadedAsset = _localizeLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Sound)
                {
                    loadedAsset = _soundLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Fx)
                {
                    loadedAsset = _fxLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.ImpactFx)
                {
                    loadedAsset = _impactFxLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.XAnim)
                {
                    loadedAsset = _xanimLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.XModel)
                {
                    loadedAsset = _xmodelLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.PhysCollmap)
                {
                    loadedAsset = _physCollmapLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Font)
                {
                    loadedAsset = _fontLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.Vehicle)
                {
                    loadedAsset = _vehicleLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else if (asset.Type == XAssetType.LightDef)
                {
                    loadedAsset = _lightDefLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
                }
                else
                {
                    loadedAsset = _weaponLoader.LoadFromAssetPointer(cursor, asset.AssetPointer.Untyped, context);
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
