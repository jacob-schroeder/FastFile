using System.Runtime.InteropServices;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Explicit, Pack = 4)]
public struct EventData
{
    [FieldOffset(0)]
    public ZonePointer<string> UnconditionalScript;

    [FieldOffset(0)]
    public ZonePointer<ConditionalScript> ConditionalScript;

    [FieldOffset(0)]
    public ZonePointer<MenuEventHandlerSet> ElseScript;

    [FieldOffset(0)]
    public ZonePointer<SetLocalVarData> SetLocalVarData;
}