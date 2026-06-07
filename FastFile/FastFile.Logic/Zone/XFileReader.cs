using FastFile.Logic.Assets.Readers;
using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Models.Assets;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
{
    private static readonly bool TraceAssets = IsTraceEnabled("FASTFILE_TRACE_ASSETS");
    private static readonly int TraceAssetLimit = GetTraceLimit("FASTFILE_TRACE_ASSET_LIMIT");
    private static int _traceAssetCount;

    private readonly ReadOnlyMemory<byte> _memory = buffer.AsMemory();
    private readonly IList<string> _warnings = new List<string>();
    private int _position;
    private XFileReadStreamBlocks? _streamBlocks;

    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    private ReadOnlySpan<byte> Span => _memory.Span;

    public XFile ParseHeader()
    {
        var context = new XFileReadContext(Span, _position);
        var header = new XFile
        {
            Size = context.ReadInt32(),
            ExternalSize = context.ReadInt32(),
            BlockSize = new int[(int)XFILE_BLOCK.MAX_XFILE_COUNT]
        };

        for (var i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
            header.BlockSize[i] = context.ReadInt32();

        _position = context.Position;
        _streamBlocks = new XFileReadStreamBlocks(header, _position);
        return header;
    }

    public XAssetList ParseXAssetList()
    {
        if (_streamBlocks is null)
            throw new InvalidOperationException("ParseHeader must be called before ParseXAssetList.");

        var context = new XFileReadContext(Span, _position, _streamBlocks, _warnings);

        XAssetList assetList;
        assetList = ReadXAssetList(ref context, assetReadProgress);
        context.FinalizeOffsetPointerBindings();

        _position = context.Position;

        return assetList;
    }

    private static XAssetList ReadXAssetList(
        ref XFileReadContext context,
        Action<int, int>? assetReadProgress)
    {
        var scriptStringCount = context.ReadInt32();
        var scriptStringsPtr = context.ReadDirectPointer<ZonePointer<string?>[]>("XAssetList.ScriptStrings");
        var assetCount = context.ReadInt32();
        var assetsPtr = context.ReadDirectPointer<XAsset[]>("XAssetList.Assets");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            context.PushStreamBlock(XFILE_BLOCK.TEMP);
            try
            {
                context.ResolveInlinePointerNow(
                    scriptStringsPtr,
                    (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<string?>[]> pointer) =>
                    {
                        var pointers = ReadScriptStringPointers(ref pointerContext, scriptStringCount);
                        pointer.SetResult(pointers);

                        foreach (var scriptStringPointer in pointers)
                        {
                            if (scriptStringPointer.CanMaterializeInline)
                            {
                                var scriptString = pointerContext.ReadPointerValue(
                                    scriptStringPointer,
                                    GenericReader.ReadCString);

                                scriptStringPointer.SetResult(scriptString);
                                continue;
                            }

                            scriptStringPointer.SetResult(default);
                        }
                    });
            }
            finally
            {
                context.PopStreamBlock();
            }

            context.ResolveInlinePointerNow(
                assetsPtr,
                (ref XFileReadContext pointerContext, ZonePointer<XAsset[]> pointer) =>
                {
                    var assets = ReadAssets(ref pointerContext, assetCount, assetReadProgress);
                    pointer.SetResult(assets);
                });

            context.ResolveQueued();
        }
        finally
        {
            context.PopStreamBlock();
        }

        return new XAssetList
        {
            ScriptStringCount = scriptStringCount,
            ScriptStringsPtr = scriptStringsPtr,
            AssetCount = assetCount,
            AssetsPtr = assetsPtr,
        };
    }

    private static ZonePointer<string?>[] ReadScriptStringPointers(
        ref XFileReadContext context,
        int count)
    {
        var pointers = new ZonePointer<string?>[count];
        for (var i = 0; i < count; i++)
            pointers[i] = context.ReadDirectPointer<string?>($"XAssetList.ScriptStrings[{i}]");

        return pointers;
    }

    private static XAsset[] ReadAssets(
        ref XFileReadContext context,
        int count,
        Action<int, int>? assetReadProgress)
    {
        var assets = new XAsset[count];
        for (var i = 0; i < count; i++)
            assets[i] = ReadAsset(ref context, i, count, assetReadProgress);

        return assets;
    }

    private static XAsset ReadAsset(
        ref XFileReadContext context,
        int index,
        int assetCount,
        Action<int, int>? assetReadProgress)
    {
        var rowOffset = context.Position;
        var type = (XAssetType)context.ReadInt32();

        return new XAsset
        {
            Type = type,
            XAssetPtr = ReadAssetPointer(ref context, type, index, assetCount, rowOffset, assetReadProgress),
        };
    }

    private static ZonePointer<BaseAsset> ReadAssetPointer(
        ref XFileReadContext context,
        XAssetType type,
        int index,
        int assetCount,
        int rowOffset,
        Action<int, int>? assetReadProgress)
    {
        var pointerOffset = context.Position;
        var pointer = context.ReadAliasPointer<BaseAsset>($"XAsset[{index}].Header");
        TraceAsset(
            $"asset[{index:D5}] table=0x{rowOffset:X8} headerField=0x{pointerOffset:X8} "
            + $"type=0x{(int)type:X2}/{type} raw=0x{pointer.Raw:X8}");

        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<BaseAsset> resolvedPointer) =>
            {
                var start = pointerContext.Position;
                var streamStart = pointerContext.GetActiveStreamAddress();
                TraceAsset(
                    $"asset[{index:D5}] begin type=0x{(int)type:X2}/{type} "
                    + $"src=0x{start:X8} stream=b{streamStart.BlockIndex}:0x{streamStart.Offset:X8} "
                    + $"raw=0x{resolvedPointer.Raw:X8}");

                if (!XAssetReaderRegistry.TryGetReader(type, out var reader))
                {
                    resolvedPointer.SetResult(new UnknownAsset(type)
                    {
                        Offset = pointerContext.Position
                    });
                    TraceAsset(
                        $"asset[{index:D5}] end type=0x{(int)type:X2}/{type} "
                        + $"src=0x{start:X8}/0x{pointerContext.Position - start:X} resolved=UnknownAsset");
                    ReportAssetReadProgress(assetReadProgress, index, assetCount);
                    return;
                }

                try
                {
                    pointerContext.SetCurrentAsset(index, type);
                    var asset = reader(ref pointerContext);
                    resolvedPointer.SetResult(asset);
                    TraceAsset(
                        $"asset[{index:D5}] end type=0x{(int)type:X2}/{type} "
                        + $"src=0x{start:X8}/0x{pointerContext.Position - start:X} "
                        + $"resolved={asset.GetType().Name}");
                    ReportAssetReadProgress(assetReadProgress, index, assetCount);
                }
                catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
                {
                    TraceAsset(
                        $"asset[{index:D5}] fail type=0x{(int)type:X2}/{type} "
                        + $"src=0x{start:X8} current=0x{pointerContext.Position:X8} "
                        + $"raw=0x{resolvedPointer.Raw:X8} error={ex.GetType().Name}");
                    throw new InvalidDataException(
                        $"Failed to read asset[{index}] {type} at zone offset 0x{pointerContext.Position:X8} ({pointerContext.Position:N0}); raw pointer=0x{resolvedPointer.Raw:X8}.",
                        ex);
                }
            });

        if (pointer.IsResolved)
            ReportAssetReadProgress(assetReadProgress, index, assetCount);

        return pointer;
    }

    private static void ReportAssetReadProgress(
        Action<int, int>? assetReadProgress,
        int assetIndex,
        int assetCount)
    {
        assetReadProgress?.Invoke(assetIndex + 1, assetCount);
    }

    private static bool IsTraceEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }

    private static int GetTraceLimit(string name)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value >= 0
            ? value
            : int.MaxValue;
    }

    private static void TraceAsset(string message)
    {
        if (!TraceAssets || _traceAssetCount >= TraceAssetLimit)
            return;

        _traceAssetCount++;
        Console.Error.WriteLine($"[asset-trace] {message}");
    }
}
