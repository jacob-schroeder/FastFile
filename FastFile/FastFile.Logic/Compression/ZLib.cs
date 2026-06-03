using Ionic.Zlib;

namespace FastFile.Logic.Compression;

public class ZLib
{
    public const int HEADER_SIZE = 2;
    public const ushort ADLR32_SIZE = 4;
    
    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var stream =
            new Ionic.Zlib.ZlibStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        stream.CopyTo(output);

        return output.ToArray();
    }

    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var stream = new Ionic.Zlib.ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression))
        {
            stream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }
}
