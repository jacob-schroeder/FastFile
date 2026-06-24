using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class EventData
{
    public const int SerializedSize = 0x04;

    public XPointerReference Data { get; init; }
    public int Raw => Data.Raw;

    public XString UnconditionalScript => Data.AsPointer<string>();
    public XPointer<ConditionalScript> ConditionalScript => Data.AsPointer<ConditionalScript>();
    public XPointer<MenuEventHandlerSet> ElseScript => Data.AsPointer<MenuEventHandlerSet>();
    public XPointer<SetLocalVarData> SetLocalVarData => Data.AsPointer<SetLocalVarData>();
}
