using System.Buffers.Binary;
using FastFile.Logic.Assets.Writers;
using FastFile.Models.Data;
using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class ZoneWriter(XFile header, XAssetList assetList, int? paddedLength = null)
{
    public byte[] Write()
    {
        var context = new ZoneWriterContext();

        WriteHeader(context, size: 0);
        WriteXAssetList(context, assetList);
        context.ResolveQueued();

        var computedSize = context.Position;
        var meaningfulSize = paddedLength is not null && header.Size > 0 && header.Size <= computedSize
            ? header.Size
            : computedSize;
        if (paddedLength is { } length && length > context.Position)
            context.WriteBytes(new byte[length - context.Position]);

        var buffer = context.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), meaningfulSize);
        header.Size = meaningfulSize;

        return buffer;
    }

    private void WriteHeader(ZoneWriterContext context, int size)
    {
        context.WriteInt32(size);
        context.WriteInt32(header.ExternalSize);

        for (var i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
        {
            var blockSize = i < header.BlockSize.Length ? header.BlockSize[i] : 0;
            context.WriteInt32(blockSize);
        }
    }

    private static void WriteXAssetList(ZoneWriterContext context, XAssetList value)
    {
        context.WriteInt32(value.ScriptStringCount);
        context.WritePointer(value.ScriptStringsPtr, WriteScriptStringPointers);
        context.WriteInt32(value.AssetCount);
        context.WritePointer(value.AssetsPtr, WriteAssets);
    }

    private static void WriteScriptStringPointers(
        ZoneWriterContext context,
        ZonePointer<ZonePointer<string?>[]> pointer)
    {
        foreach (var scriptStringPointer in pointer.Result ?? [])
            context.WritePointer(scriptStringPointer, WriteScriptStringPointerValue);
    }

    private static void WriteScriptStringPointerValue(
        ZoneWriterContext context,
        ZonePointer<string?> pointer)
    {
        context.WriteCString(pointer.Result);
    }

    private static void WriteAssets(
        ZoneWriterContext context,
        ZonePointer<XAsset[]> pointer)
    {
        foreach (var asset in pointer.Result ?? [])
            WriteAsset(context, asset);
    }

    private static void WriteAsset(ZoneWriterContext context, XAsset asset)
    {
        context.WriteInt32((int)asset.Type);
        context.WritePointer(asset.XAssetPtr, (assetContext, pointer) =>
        {
            if (pointer.Result is not { } value)
                return;

            WriteAssetValue(assetContext, value);
        });
    }

    private static void WriteAssetValue(ZoneWriterContext context, BaseAsset asset)
    {
        if (!XAssetWriterRegistry.TryGetWriter(asset.Type, out var writer))
            throw new InvalidDataException($"No zone writer registered for asset type {asset.Type}.");

        writer(context, asset);
    }
}
