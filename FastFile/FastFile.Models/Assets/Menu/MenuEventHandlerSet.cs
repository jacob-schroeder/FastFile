using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class MenuEventHandlerSet
{
    public const int SerializedSize = 0x08;

    public int EventHandlerCount { get; init; }
    public XPointer<XPointer<MenuEventHandler>[]> EventHandlers { get; init; }
    public IReadOnlyList<MenuEventHandlerReference> Handlers { get; set; } = [];
}

public sealed record MenuEventHandlerReference(
    int Index,
    XPointer<MenuEventHandler> Pointer,
    MenuEventHandler? Handler);
