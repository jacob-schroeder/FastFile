using System.IO.Compression;

namespace FastFile.Logic.Compression;

public static class Deflate
{
    public static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var stream =
            new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        using var output = new MemoryStream();

        stream.CopyTo(output);

        return output.ToArray();
    }
}