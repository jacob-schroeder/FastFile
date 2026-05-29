using System.Runtime.InteropServices;
using FastFile.Models.Assets.Menu.Elements;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MenuEventHandler
{
    public EventData EventData;
    public byte EventType;
}