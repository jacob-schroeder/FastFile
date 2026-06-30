using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.GameMap;

public sealed class GameWorldMpAsset : BaseAsset
{
    public const int SerializedSize = 0x08;

    public XAssetType Type => XAssetType.GameMapMp;

    // 0x00: XString. PS3 GameWorldMp body stores root+0x00 into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }

    // 0x04: G_GlassData*. PS3 allocates inline glass data when this cell is non-null.
    public XPointer<GGlassData> GlassDataPointer { get; init; }
    public GGlassData? GlassData { get; init; }
}

public sealed class GGlassData
{
    public const int SerializedSize = 0x80;

    public int Offset { get; init; }

    // 0x00: G_GlassPiece*. PS3 loads pieceCount fixed 0x0C-byte rows when non-null.
    public XPointer<GGlassPiece[]> GlassPiecesPointer { get; init; }
    public IReadOnlyList<GGlassPiece> GlassPieces { get; init; } = [];

    // 0x04: G_GlassData.pieceCount. PS3 uses this as the G_GlassPiece array count.
    public int PieceCount { get; init; }

    // 0x08..0x0B: Xbox-correlated G_GlassData damage thresholds.
    public ushort DamageToWeaken { get; init; }
    public ushort DamageToDestroy { get; init; }

    // 0x0C: G_GlassData.glassNameCount. PS3 uses this as the G_GlassName array count.
    public int GlassNameCount { get; init; }

    // 0x10: G_GlassName*. PS3 loads glassNameCount fixed 0x0C-byte rows when non-null.
    public XPointer<GGlassName[]> GlassNamesPointer { get; init; }
    public IReadOnlyList<GGlassName> GlassNames { get; init; } = [];

    // 0x14..0x7F: Xbox-correlated G_GlassData.pad byte array; copied by the fixed 0x80 PS3 root.
    public IReadOnlyList<byte> Pad14To7F { get; init; } = [];
}

public sealed class GGlassPiece
{
    public const int SerializedSize = 0x0C;

    public int Offset { get; init; }

    // 0x00..0x07: Xbox-correlated G_GlassPiece state fields.
    public ushort DamageTaken { get; init; }
    public ushort CollapseTime { get; init; }
    public int LastStateChangeTime { get; init; }

    // 0x08..0x0B: Xbox names impactDir and impactPos; PS3 packing width is preserved here.
    public ushort PackedImpactDir { get; init; }
    public ushort PackedImpactPos { get; init; }
}

public sealed class GGlassName
{
    public const int SerializedSize = 0x0C;

    public int Offset { get; init; }

    // 0x00: XString nameStr. PS3 stores row+0x00 into varXString and calls Load_XString.
    public XPointer<string> NameStrPointer { get; init; }
    public string? NameStr { get; init; }

    // 0x04: G_GlassName.name. Xbox-correlated script string index copied by the fixed row load.
    public ushort Name { get; init; }

    // 0x06: G_GlassName.pieceCount. PS3 uses this as the pieceIndices ushort count.
    public ushort PieceCount { get; init; }

    // 0x08: ushort*. PS3 aligns to 2 and loads pieceCount indices when non-null.
    public XPointer<ushort[]> PieceIndicesPointer { get; init; }
    public IReadOnlyList<ushort> PieceIndices { get; init; } = [];
}
