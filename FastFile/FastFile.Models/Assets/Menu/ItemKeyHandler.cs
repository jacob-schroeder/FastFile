using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ItemKeyHandler
{
    public const int SerializedSize = 0x0c;

    public int Key { get; init; }
    public XPointer<MenuEventHandlerSet> Action { get; init; }
    public XPointer<ItemKeyHandler> Next { get; init; }
}
