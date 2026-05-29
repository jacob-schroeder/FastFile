using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct ItemKeyHandler
{
    public int Key;
    public ZonePointer<MenuEventHandlerSet> Action;
    public ZonePointer<ItemKeyHandler> Next;
}