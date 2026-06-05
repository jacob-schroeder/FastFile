using System.Buffers.Binary;
using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private const int XFileHeaderSize = 0x24;
    private const int Ps3TempBlockSize = 0x3B4;
    private const int Ps3VertexBlockSize = 0x1000;
    private const int WeaponVariantDefSize = 0x74;
    private const int WeaponDefSize = 0x684;

    private readonly XFile _sourceHeader;
    private readonly XAssetList _assetList;

    public XFileWriter(XFile header, XAssetList assetList)
    {
        _sourceHeader = CloneHeader(header);
        _assetList = assetList;
    }

    public XFileWriteResult Write()
    {
        return Write(WriteXAssetList);
    }

    public XFileWriteResult Write(Action<XFileWriterContext, XAssetList> writeAssetList)
    {
        var context = new XFileWriterContext(_sourceHeader);
        writeAssetList(context, _assetList);
        context.ResolveOffsetPointerFields();
        return Complete(context);
    }

    public XFileWriteResult CreateEmpty()
    {
        return Complete(new XFileWriterContext(_sourceHeader));
    }

    private XFileWriteResult Complete(XFileWriterContext context)
    {
        var serializedBlockSizes = context.GetBlockSizes();
        var serializedContent = context.ToArray();
        var contentLengthAfterHeader = serializedContent.Length;
        var size = GetXFileSize(contentLengthAfterHeader);
        var headerBlockSizes = serializedBlockSizes.ToArray();
        SetBlockSize(headerBlockSizes, XFILE_BLOCK.TEMP, Ps3TempBlockSize);
        SetBlockSize(
            headerBlockSizes,
            XFILE_BLOCK.LARGE,
            GetLargeBlockSize(size));
        SetBlockSize(headerBlockSizes, XFILE_BLOCK.XFILE_BLOCK_VERTEX, Ps3VertexBlockSize);

        var blockBytes = context.GetBlockBytes(headerBlockSizes);
        var header = new XFile
        {
            Size = size,
            ExternalSize = _sourceHeader.ExternalSize,
            BlockSize = headerBlockSizes,
        };

        return new XFileWriteResult(header, blockBytes, BuildLinearZoneBuffer(header, serializedContent));
    }

    private static byte[] BuildLinearZoneBuffer(XFile header, byte[] serializedContent)
    {
        using var stream = new MemoryStream();
        WriteInt32(stream, header.Size);
        WriteInt32(stream, header.ExternalSize);

        for (var i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
        {
            var blockSize = i < header.BlockSize.Length ? header.BlockSize[i] : 0;
            WriteInt32(stream, blockSize);
        }

        stream.Write(serializedContent);

        PadToFastFileBlockBoundary(stream);
        return stream.ToArray();
    }

    private static void PadToFastFileBlockBoundary(Stream stream)
    {
        const int fastFileBlockSize = 0x10000;
        var remainder = stream.Length % fastFileBlockSize;
        if (remainder == 0)
            return;

        stream.Write(new byte[fastFileBlockSize - remainder]);
    }

    private static void SetBlockSize(int[] blockSizes, XFILE_BLOCK block, int size)
    {
        var index = (int)block;
        if (index >= 0 && index < blockSizes.Length)
            blockSizes[index] = size;
    }

    private static int GetXFileSize(int contentLengthAfterHeader)
    {
        return contentLengthAfterHeader;
    }

    private static int GetBlockSize(IReadOnlyList<int> blockSizes, XFILE_BLOCK block)
    {
        var index = (int)block;
        return index >= 0 && index < blockSizes.Count
            ? blockSizes[index]
            : 0;
    }

    private int GetSourceBlockSize(XFILE_BLOCK block)
    {
        var index = (int)block;
        return index >= 0 && index < _sourceHeader.BlockSize.Length
            ? _sourceHeader.BlockSize[index]
            : 0;
    }

    private int GetLargeBlockSize(int xfileSize)
    {
        var sourceLargeSize = GetSourceBlockSize(XFILE_BLOCK.LARGE);
        var sizeDelta = xfileSize - _sourceHeader.Size;
        return Math.Max(0, sourceLargeSize + sizeDelta);
    }

    private static void WriteInt32(Stream stream, int value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static XFile CloneHeader(XFile value)
    {
        return new XFile
        {
            Size = value.Size,
            ExternalSize = value.ExternalSize,
            BlockSize = value.BlockSize.ToArray(),
        };
    }
}
