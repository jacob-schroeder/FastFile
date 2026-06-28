using FastFile.Models.Math;
using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class WindowDef
{
    public const int SerializedSize = WindowDefContract.SerializedSize;

    public XString NamePointer { get; init; }
    public string? Name { get; set; }
    public RectangleDef Rect { get; init; } = new();
    public RectangleDef RectClient { get; init; } = new();
    public XString GroupPointer { get; init; }
    public string? Group { get; set; }
    public WindowStyle Style { get; init; }
    public WindowBorder Border { get; init; }

    /// <summary>
    /// Numeric owner-draw selector. PS3 item paint treats this as a selector value passed into owner-draw render paths.
    /// </summary>
    public WindowOwnerDraw OwnerDraw { get; init; }

    /// <summary>
    /// Numeric owner-draw visibility selector. PS3 item visibility passes this to its owner-draw visibility helper when non-zero.
    /// </summary>
    public int OwnerDrawFlags { get; init; }

    public float BorderSize { get; init; }
    public WindowStaticFlags StaticFlags { get; init; }
    public IReadOnlyList<WindowDynamicFlags> DynamicFlags { get; init; } = [];
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
    public const int SerializedSize = RectangleDefContract.SerializedSize;

    public float X { get; init; }
    public float Y { get; init; }
    public float W { get; init; }
    public float H { get; init; }
    public HorizontalAlign HorzAlign { get; init; }
    public VerticalAlign VertAlign { get; init; }
    public ushort Pad12 { get; init; }
}
