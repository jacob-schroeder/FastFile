using FastFile.Loaders.Compression;
using FastFile.Runtime.Diagnostics;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.DbFileLoad;

public sealed class XBlockReader
{
    private const ushort ZoneBlockTerminator = 1;

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

            if (blockSize == 0)
            {
                diagnostics.Warn($"Encountered invalid compressed block size 0 at 0x{cursor.Offset - 2:X}.");
                cursor.Skip(sizeof(ushort));
                continue;
            }

            int compressedSize = blockSize - sizeof(uint);
            byte[] compressed = cursor.ReadBytes(compressedSize);
            uint expectedAdler32 = cursor.ReadUInt32();

            byte[] decompressed = Deflate.Decompress(compressed);
            output.Write(decompressed);

            uint actualAdler32 = Adler32.HashToUInt32(decompressed);
            if (actualAdler32 != expectedAdler32)
                diagnostics.Warn($"Checksum mismatch at compressed block ending 0x{cursor.Offset:X}. Expected {expectedAdler32:X8}, got {actualAdler32:X8}.");
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
