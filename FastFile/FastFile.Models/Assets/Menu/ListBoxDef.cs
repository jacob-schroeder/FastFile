using FastFile.Models.Math;
using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class ListBoxDef
{
    public const int SerializedSize = 0x158;

    // PS3 runtime treats these as per-local-client mutable visible range cursors.
    public IReadOnlyList<int> StartPos { get; init; } = [];
    public IReadOnlyList<int> EndPos { get; init; } = [];

    // Loader layout slot is proven; PS3 consumer semantics are still open.
    public int DrawPadding { get; init; }

    public float ElementWidth { get; init; }
    public float ElementHeight { get; init; }
    public int ElementStyle { get; init; }
    public int NumColumns { get; init; }
    public IReadOnlyList<ColumnInfo> ColumnInfo { get; init; } = [];
    public XPointer<MenuEventHandlerSet> DoubleClick { get; init; }
    public int NotSelectable { get; init; }
    public int NoScrollbars { get; init; }

    // Loader layout slot is proven; no confirmed PS3 listbox consumer yet.
    public int UsePaging { get; init; }

    public Vec4 SelectBorder { get; init; } = new();
    public XPointer<MaterialAsset> SelectIcon { get; init; }
}

public sealed class ColumnInfo
{
    public const int SerializedSize = 0x10;

    public int Pos { get; init; }
    public int Width { get; init; }
    public int MaxChars { get; init; }
    public int Alignment { get; init; }
}
