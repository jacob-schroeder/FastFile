using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class Statement
{
    public const int SerializedSize = 0x18;

    public int NumEntries { get; init; }
    public XPointer<ExpressionEntry[]> Entries { get; init; }
    public IReadOnlyList<ExpressionEntry> LoadedEntries { get; set; } = [];
    public XPointer<ExpressionSupportingData> SupportingData { get; init; }
    public ExpressionSupportingData? SupportingDataValue { get; set; }

    // Runtime evaluator cache stamp. PS3 default_mp EvaluateExpression at 0x002332e0
    // compares this to the UI expression clock and refreshes it for non-string results.
    public int LastExecuteTime { get; init; }

    // Runtime result cache. PS3 default_mp EvaluateExpression returns &LastResult
    // and writes Operand.DataType/+Value after a successful evaluation.
    public Operand LastResult { get; init; } = new();
}

public sealed class ExpressionEntry
{
    public const int SerializedSize = 0x0c;

    public ExpressionEntryKind Kind { get; init; }
    public Operand Operand { get; init; } = new();
    public string? StringValue { get; set; }
    public Statement? FunctionStatement { get; set; }
    public OperationEnum Operation => (OperationEnum)Operand.DataType;
}
