using System.Runtime.InteropServices;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct ExpressionString
{
    public ZonePointer<string> String;
}