using FastFile.Logic.Assets.Readers;
using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Models.Assets;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class XFileReader(byte[] buffer)
{
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
        assetList = ReadXAssetList(ref context);
        context.ResolveOffsetPointers();

        _position = context.Position;

        return assetList;
    }

    private static XAssetList ReadXAssetList(ref XFileReadContext context)
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
                            if (scriptStringPointer.IsInlineData)
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
                    var assets = ReadAssets(ref pointerContext, assetCount);
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

    private static XAsset[] ReadAssets(ref XFileReadContext context, int count)
    {
        var assets = new XAsset[count];
        for (var i = 0; i < count; i++)
            assets[i] = ReadAsset(ref context, i);

        return assets;
    }

    private static XAsset ReadAsset(ref XFileReadContext context, int index)
    {
        var type = (XAssetType)context.ReadInt32();

        return new XAsset
        {
            Type = type,
            XAssetPtr = ReadAssetPointer(ref context, type, index),
        };
    }

    private static ZonePointer<BaseAsset> ReadAssetPointer(
        ref XFileReadContext context,
        XAssetType type,
        int index)
    {
        var pointer = context.ReadAliasPointer<BaseAsset>($"XAsset[{index}].Header");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<BaseAsset> resolvedPointer) =>
            {
                if (!XAssetReaderRegistry.TryGetReader(type, out var reader))
                {
                    resolvedPointer.SetResult(new UnknownAsset(type)
                    {
                        Offset = pointerContext.Position
                    });
                    return;
                }

                try
                {
                    var asset = reader(ref pointerContext);
                    resolvedPointer.SetResult(asset);
                }
                catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
                {
                    throw new InvalidDataException(
                        $"Failed to read asset[{index}] {type} at zone offset 0x{pointerContext.Position:X8} ({pointerContext.Position:N0}); raw pointer=0x{resolvedPointer.Raw:X8}.",
                        ex);
                }
            });

        return pointer;
    }
}
