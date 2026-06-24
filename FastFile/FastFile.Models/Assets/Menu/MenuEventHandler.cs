namespace FastFile.Models.Assets.Menu;

public sealed class MenuEventHandler
{
    public const int SerializedSize = 0x08;

    public EventData EventData { get; init; } = new();
    public MenuEventHandlerType EventType { get; init; }
    public byte Pad05 { get; init; }
    public byte Pad06 { get; init; }
    public byte Pad07 { get; init; }
}
