using FastFile.Logic.Assets;
using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets;
using FastFile.Models.Zone;
using FastFile.Models.Data;

namespace FastFile.Logic;

public sealed class ZoneReader(byte[] buffer)
{
    private readonly ReadOnlyMemory<byte> _memory = buffer.AsMemory();
    private int _position = 0;
    private int _length = buffer.Length;
    
    private readonly IList<string> _warnings = new List<string>();

    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();
    public Action<string, int, int>? Trace { get; set; }

    private ReadOnlySpan<byte> Span => _memory.Span;

    public XFile ParseHeader()
    {
        var context = new ZoneReadContext(Span, _position);
        var header = new XFile
        {
            Size = context.ReadInt32(),
            ExternalSize = context.ReadInt32(),
            BlockSize = new int[(int)XFILE_BLOCK.MAX_XFILE_COUNT]
        };
        
        for(int i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
            header.BlockSize[i] = context.ReadInt32();

        _position = context.Position;
        return header;
    }

    public XAssetList ParseXAssetList()
    {
        var context = new ZoneReadContext(Span, _position);
        context.Trace = Trace;
        var assetList = ReadXAssetList(ref context);
        
        context.ResolveQueued();
        _position = context.Position;

        return assetList;
    }

    private static XAssetList ReadXAssetList(ref ZoneReadContext context)
    {
        var scriptStringCount = context.ReadInt32();
        var scriptStringsPtr = context.ReadPointer<ZonePointer<string?>[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<string?>[]> pointer) =>
            {
                var pointers = ReadScriptStringPointers(ref pointerContext, scriptStringCount);
                pointer.SetResult(pointers);

                foreach (var scriptStringPointer in pointers)
                {
                    if (scriptStringPointer.Kind == PointerKind.Null)
                    {
                        scriptStringPointer.SetResult(default);
                        continue;
                    }

                    var scriptString = pointerContext.ReadPointerValue(
                        scriptStringPointer,
                        GenericReader.ReadCString);

                    scriptStringPointer.SetResult(scriptString);
                }
            });
        var assetCount = context.ReadInt32();
        var assetsPtr = context.ReadPointer<XAsset[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<XAsset[]> pointer) =>
            {
                var assets = ReadAssets(ref pointerContext, assetCount);
                pointer.SetResult(assets);
            });

        return new XAssetList
        {
            ScriptStringCount = scriptStringCount,
            ScriptStringsPtr = scriptStringsPtr,
            AssetCount = assetCount,
            AssetsPtr = assetsPtr,
        };
    }

    private static ZonePointer<string?>[] ReadScriptStringPointers(
        ref ZoneReadContext context,
        int count)
    {
        var pointers = new ZonePointer<string?>[count];
        for (var i = 0; i < count; i++)
            pointers[i] = context.ReadPointer<string?>();

        return pointers;
    }

    private static XAsset[] ReadAssets(ref ZoneReadContext context, int count)
    {
        var assets = new XAsset[count];
        for (var i = 0; i < count; i++)
            assets[i] = ReadAsset(ref context, i);

        return assets;
    }

    private static XAsset ReadAsset(ref ZoneReadContext context, int index)
    {
        var type = (XAssetType)context.ReadInt32();

        return new XAsset
        {
            Type = type,
            XAssetPtr = context.ReadPointer<BaseAsset>(
                (ref ZoneReadContext pointerContext, ZonePointer<BaseAsset> pointer) =>
                {
                    var start = pointerContext.Position;

                    if (!XAssetReaderRegistry.TryGetReader(type, out var reader))
                    {
                        pointer.SetResult(new UnknownAsset(type)
                        {
                            Offset = pointerContext.Position
                        });
                        pointerContext.Trace?.Invoke($"Asset[{index}] {type}", start, pointerContext.Position);
                        return;
                    }

                    try
                    {
                        var asset = reader(ref pointerContext);
                        pointer.SetResult(asset);
                        pointerContext.Trace?.Invoke($"Asset[{index}] {type}", start, pointerContext.Position);
                    }
                    catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
                    {
                        throw new InvalidDataException(
                            $"Failed to read asset[{index}] {type} at zone offset 0x{pointerContext.Position:X8} ({pointerContext.Position:N0}); raw pointer=0x{pointer.Raw:X8}.",
                            ex);
                    }
                }),
        };
    }
}
