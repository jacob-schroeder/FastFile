using System.Runtime.InteropServices;
using FastFile.Models.Assets.Menu.Enums;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Operand
{
    public ExpDataType DataType;
    public OperandInternalData Internals;
}