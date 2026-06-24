using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ConditionalScript
{
    public const int SerializedSize = 0x08;

    public XPointer<MenuEventHandlerSet> EventHandlerSet { get; init; }
    public XPointer<Statement> EventExpression { get; init; }
}
