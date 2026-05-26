//using System.IO.Compression;
using Ionic.Zlib;

namespace FastFile.Logic.Compression;

public class ZLib
{
    public static byte[] DecompressZlib(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var stream =
            new Ionic.Zlib.ZlibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        stream.CopyTo(output);

        return output.ToArray();
    }
}