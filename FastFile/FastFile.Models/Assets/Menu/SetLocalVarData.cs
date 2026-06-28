using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class SetLocalVarData
{
    public const int SerializedSize = 0x08;

    public XString LocalVarName { get; init; }
    public string? LocalVarNameString { get; set; }
    public XPointer<Statement> Expression { get; init; }
    public Statement? ExpressionStatement { get; set; }
}
