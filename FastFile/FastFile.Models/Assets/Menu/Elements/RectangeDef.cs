using System.Runtime.InteropServices;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct RectangleDef
{
    public float X;
    public float Y;
    public float W;
    public float H;
    public byte HorzAlign;
    public byte VertAlign;
}