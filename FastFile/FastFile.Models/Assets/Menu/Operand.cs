using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class Operand
{
    public const int SerializedSize = 0x08;

    public ExpDataType DataType { get; init; }
    public OperandInternalData Internals { get; init; }
}

public readonly record struct OperandInternalData(int Raw)
{
    public int IntVal => Raw;
    public float FloatVal => BitConverter.Int32BitsToSingle(Raw);
    public XPointer<ExpressionString> String => new(Raw, XPointerResolutionMode.Direct);
    public XPointer<Statement> Function => new(Raw, XPointerResolutionMode.Direct);
}
