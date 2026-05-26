using System.IO.Compression;

namespace FastFile.Logic.Compression;

public static class Deflate
{
    public static bool TryDecompress(byte[] data, out byte[] output)
    {
        output = [];
        
        using var input = new MemoryStream(data);
        DeflateStream stream;

        try
        {
            stream = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress);
        }
        catch (Exception ex)
        {
            //do something with warnings...
            return false;
        }

        using var outStream = new MemoryStream();
        stream.CopyTo(outStream);
        output = outStream.ToArray();
        
        return true;
    }
    
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