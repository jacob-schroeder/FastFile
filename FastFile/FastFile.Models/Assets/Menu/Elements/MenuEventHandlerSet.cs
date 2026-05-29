using FastFile.Models.Data;
using FastFile.Models.Assets.Menu.Elements;

namespace FastFile.Models.Assets.Menu.Elements;

public struct MenuEventHandlerSet
{
    int eventHandlerCount;
    ZonePointer<ZonePointer<MenuEventHandler>[]> eventHandlers;
}