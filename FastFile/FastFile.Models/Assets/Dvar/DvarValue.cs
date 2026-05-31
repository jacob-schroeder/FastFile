using System.Runtime.InteropServices;
using FastFile.Models.Data;

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct DvarValue
{
    [FieldOffset(0)]
    public int Enabled;

    [FieldOffset(0)]
    public int Integer;

    [FieldOffset(0)]
    public uint UnsignedInt;

    [FieldOffset(0)]
    public float Value;

    [FieldOffset(0)]
    public fixed float Vector[4];

    [FieldOffset(0)]
    public uint StringPtr;

    [FieldOffset(0)]
    public fixed byte Color[4];
}
