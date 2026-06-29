using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class Operand
{
    public const int SerializedSize = 0x08;

    public ExpDataType DataType { get; init; }
    public OperandValue Value { get; init; } = new IntOperandValue(0);
    public int EncodedValue => Value.EncodedValue;
}

public abstract record OperandValue(int EncodedValue);

public sealed record IntOperandValue(int Value) : OperandValue(Value);

public sealed record FloatOperandValue(float Value, int EncodedBits) : OperandValue(EncodedBits);

public sealed record StringOperandValue(XPointer<string> StringPointer) : OperandValue(StringPointer.Raw);

public sealed record FunctionOperandValue(XPointer<Statement> StatementPointer) : OperandValue(StatementPointer.Raw);

public sealed record ReservedOperandValue(int Reserved) : OperandValue(Reserved);

public static class OperandValueFactory
{
    public static OperandValue FromEncoded(ExpDataType dataType, int encodedValue)
    {
        return dataType switch
        {
            ExpDataType.VAL_INT => new IntOperandValue(encodedValue),
            ExpDataType.VAL_FLOAT => new FloatOperandValue(BitConverter.Int32BitsToSingle(encodedValue), encodedValue),
            ExpDataType.VAL_STRING => new StringOperandValue(new XPointer<string>(encodedValue, XPointerResolutionMode.Direct)),
            ExpDataType.VAL_FUNCTION => new FunctionOperandValue(new XPointer<Statement>(encodedValue, XPointerResolutionMode.Direct)),
            _ => new ReservedOperandValue(encodedValue)
        };
    }
}
