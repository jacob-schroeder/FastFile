using System.Runtime.InteropServices;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Explicit, Pack = 4)]
public struct OperandInternalData
{
    [FieldOffset(0)]
    public int IntValue;
    
    [FieldOffset(0)]
    public float FloatValue;
    
    [FieldOffset(0)]
    public ZonePointer<ExpressionString> StringValue;
    
    [FieldOffset(0)]
    public ZonePointer<Statement_s> Function;
}