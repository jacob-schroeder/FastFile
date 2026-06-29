using FastFile.Loaders.Compression;
using FastFile.Runtime.Diagnostics;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.DbFileLoad;

public sealed class XBlockReader
{
    private const ushort ZoneBlockTerminator = 1;
    private const int FullBlockSize = 0x10000;

    public byte[] ReadZone(FastFileCursor cursor, uint fileSize, LoadDiagnostics diagnostics)
    {
        if (fileSize > int.MaxValue)
            throw new InvalidDataException($"FileSize 0x{fileSize:X} does not fit in this reader.");

        using var output = new MemoryStream();
        int packedEnd = checked((int)fileSize);

        while (cursor.Offset < packedEnd)
        {
            ushort blockSize = cursor.ReadUInt16();

            if (blockSize == ZoneBlockTerminator)
            {
                TryConsumeTrailingTerminatorWord(cursor);
                break;
            }

            int compressedSize = blockSize == 0 ? FullBlockSize : blockSize;
            byte[] compressed = cursor.ReadBytes(compressedSize);

            if (blockSize == 0)
            {
                output.Write(compressed);
                continue;
            }

            byte[] decompressed = Deflate.Decompress(compressed);
            output.Write(decompressed);
        }

        return output.ToArray();
    }

    private static void TryConsumeTrailingTerminatorWord(FastFileCursor cursor)
    {
        if (cursor.Remaining < sizeof(ushort))
            return;

        if (cursor.PeekUInt16() == ZoneBlockTerminator)
            cursor.Skip(sizeof(ushort));
    }
}
