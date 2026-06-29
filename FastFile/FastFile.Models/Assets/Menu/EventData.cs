using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class EventData
{
    public const int SerializedSize = 0x04;

    public EventDataValue Value { get; init; } = new IgnoredEventData { Reserved = 0 };

    public UnconditionalScriptEventData? UnconditionalScript => Value as UnconditionalScriptEventData;
    public ConditionalScriptEventData? ConditionalScript => Value as ConditionalScriptEventData;
    public ElseScriptEventData? ElseScript => Value as ElseScriptEventData;
    public SetLocalVarEventData? SetLocalVarData => Value as SetLocalVarEventData;
}

public abstract class EventDataValue
{
}

public sealed class UnconditionalScriptEventData : EventDataValue
{
    public XString Script { get; init; }
}

public sealed class ConditionalScriptEventData : EventDataValue
{
    public XPointer<ConditionalScript> ConditionalScriptPointer { get; init; }
}

public sealed class ElseScriptEventData : EventDataValue
{
    public XPointer<MenuEventHandlerSet> EventHandlerSetPointer { get; init; }
}

public sealed class SetLocalVarEventData : EventDataValue
{
    public XPointer<SetLocalVarData> SetLocalVarDataPointer { get; init; }
}

public sealed class IgnoredEventData : EventDataValue
{
    public int Reserved { get; init; }
}
