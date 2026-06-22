using System.Runtime.InteropServices;

namespace FastFile.Models.Pointers;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public struct XArray<T>
{
    public int Value;
}
