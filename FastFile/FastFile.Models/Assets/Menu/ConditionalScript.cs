using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ConditionalScript
{
    public const int SerializedSize = 0x08;

    public XPointer<MenuEventHandlerSet> EventHandlerSet { get; init; }
    public MenuEventHandlerSet? EventHandlers { get; set; }
    public XPointer<Statement> EventExpression { get; init; }
    public Statement? EventStatement { get; set; }
}
