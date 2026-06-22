using System.Runtime.InteropServices;
using FastFile.Models.Pointers.Enums;

namespace FastFile.Models.Pointers;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct XPointer<T>(int value)
{
    public int Value = value;

    public PointerType Type => XPointerCodec.GetType(Value);
}
