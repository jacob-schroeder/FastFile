using System.Runtime.InteropServices;
using FastFile.Models.Assets.Menu.Enums;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct EntryInternalData
{
    [FieldOffset(0)]
    public OperationEnum Op;

    [FieldOffset(0)]
    public Operand Operand;
}