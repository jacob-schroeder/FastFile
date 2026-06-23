using FastFile.Models.Math;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class WindowDef
{
    public XString NamePointer { get; init; }
    public RectangleDef Rect { get; init; } = new();
    public RectangleDef RectClient { get; init; } = new();
    public XString GroupPointer { get; init; }
    public int Style { get; init; }
    public int Border { get; init; }
    public int OwnerDraw { get; init; }
    public int OwnerDrawFlags { get; init; }
    public float BorderSize { get; init; }
    public int StaticFlags { get; init; }
    public IReadOnlyList<int> DynamicFlags { get; init; } = [];
    public int NextTime { get; init; }
    public Vec4 ForeColor { get; init; } = new();
    public Vec4 BackColor { get; init; } = new();
    public Vec4 BorderColor { get; init; } = new();
    public Vec4 OutlineColor { get; init; } = new();
    public Vec4 DisableColor { get; init; } = new();
    public XPointer<MaterialAsset> Background { get; init; }
}

public sealed class RectangleDef
{
    public float X { get; init; }
    public float Y { get; init; }
    public float W { get; init; }
    public float H { get; init; }
    public HorizontalAlign HorzAlign { get; init; }
    public VerticalAlign VertAlign { get; init; }
    public ushort Pad12 { get; init; }
}
