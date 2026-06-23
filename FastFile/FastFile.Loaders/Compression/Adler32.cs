namespace FastFile.Loaders.Compression;

internal static class Adler32
{
    public static uint HashToUInt32(byte[] data)
    {
        const uint modAdler = 65521;

        uint a = 1;
        uint b = 0;

        foreach (byte value in data)
        {
            a = (a + value) % modAdler;
            b = (b + a) % modAdler;
        }

        return (b << 16) | a;
    }
}
