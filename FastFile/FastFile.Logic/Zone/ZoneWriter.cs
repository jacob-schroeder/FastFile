using System.Buffers.Binary;
using FastFile.Logic.Assets.Writers;
using FastFile.Models.Data;
using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed class ZoneWriter(XFile header, XAssetList assetList, int? paddedLength = null)
{
    private const int XFileHeaderSize = 0x24;
    private const int XFileBlockSizesOffset = 0x08;
    private const int XFileBlockSizeFieldSize = 4;
    private const int XFileCountedTrailingByteSize = 1;
    private const int XAssetTypeFieldSize = 4;
    private const int Ps3ZoneStreamOverheadSize = 0xFF3B;

    public byte[] Write()
    {
        var largeBlockBase = GetLargeBlockBase();
        var context = new ZoneWriterContext(new ZonePointerRebaser(header));

        WriteHeader(context, size: 0);
        WriteXAssetList(context, assetList);
        context.ResolveQueued();

        var computedSize = context.Position;
        var meaningfulSize = GetMeaningfulSize(computedSize);
        var largeBlockSize = GetLargeBlockSize(meaningfulSize, largeBlockBase);
        var blockSizes = GetBlockSizes(largeBlockSize);
        context.RebaseOffsetPointers(meaningfulSize, blockSizes);

        if (paddedLength is { } length && length > context.Position)
            context.WriteBytes(new byte[length - context.Position]);

        var buffer = context.ToArray();
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(0, 4), meaningfulSize);
        for (var i = 0; i < blockSizes.Length; i++)
        {
            BinaryPrimitives.WriteInt32BigEndian(
                buffer.AsSpan(XFileBlockSizesOffset + i * XFileBlockSizeFieldSize, XFileBlockSizeFieldSize),
                blockSizes[i]);
        }

        header.Size = meaningfulSize;
        header.BlockSize = blockSizes;

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

    private int GetLargeBlockSize(int xfileSize, int largeBlockBase)
    {
        // LARGE is a stream-block allocation size, not the linear zone length.
        return Math.Max(0, xfileSize - largeBlockBase);
    }

    private int GetLargeBlockBase()
    {
        return Ps3ZoneStreamOverheadSize
            + assetList.AssetCount * XAssetTypeFieldSize
            + GetNonLargeBlockSize();
    }

    private int GetNonLargeBlockSize()
    {
        var size = 0;
        for (var i = 0; i < header.BlockSize.Length; i++)
        {
            if (i == (int)XFILE_BLOCK.LARGE)
                continue;

            size += header.BlockSize[i];
        }

        return size;
    }

    private int[] GetBlockSizes(int largeBlockSize)
    {
        var blockSizes = header.BlockSize.ToArray();
        if ((int)XFILE_BLOCK.LARGE < blockSizes.Length)
            blockSizes[(int)XFILE_BLOCK.LARGE] = largeBlockSize;

        return blockSizes;
    }

    private static int GetMeaningfulSize(int computedSize)
    {
        if (computedSize < XFileHeaderSize)
            throw new InvalidDataException($"Written zone length 0x{computedSize:X8} is smaller than the 0x{XFileHeaderSize:X} byte XFile header.");

        return computedSize - XFileHeaderSize + XFileCountedTrailingByteSize;
    }

}
