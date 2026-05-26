namespace FastFile.Logic.Hashing;

public static class Adler32
{
    public static uint HashToUInt32(byte[] data)
    {
        const uint MOD_ADLER = 65521;

        uint a = 1;
        uint b = 0;

        foreach (byte value in data)
        {
            a = (a + value) % MOD_ADLER;
            b = (b + a) % MOD_ADLER;
        }

        return (b << 16) | a;
    }
}