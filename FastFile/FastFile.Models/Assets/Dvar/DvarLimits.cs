using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 0x8)]
public struct DvarLimits
{
    [FieldOffset(0)]
    public DvarEnumerationLimits Enumeration;

    [FieldOffset(0)]
    public DvarIntegerLimits Integer;

    [FieldOffset(0)]
    public DvarFloatLimits Value;

    [FieldOffset(0)]
    public DvarFloatLimits Vector;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x8)]
public struct DvarEnumerationLimits
{
    public int StringCount;

    // const char**
    public uint StringsPtr;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x8)]
public struct DvarIntegerLimits
{
    public int Min;
    public int Max;
}

[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0x8)]
public struct DvarFloatLimits
{
    public float Min;
    public float Max;
}