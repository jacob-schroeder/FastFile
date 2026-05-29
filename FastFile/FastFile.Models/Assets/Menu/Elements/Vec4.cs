using System.Runtime.InteropServices;

namespace FastFile.Models.Assets.Menu.Elements;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vec4
{
    public float A;
    public float R;
    public float G;
    public float B;
}