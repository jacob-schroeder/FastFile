using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct ConditionalScript
{
    public ZonePointer<MenuEventHandlerSet> EventHandlerSet;
    public ZonePointer<Statement_s> EventExpression; //load this first?
}