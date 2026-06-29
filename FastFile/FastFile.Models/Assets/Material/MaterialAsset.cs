using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Material;

public sealed class MaterialAsset : BaseAsset
{
    public const int SerializedSize = 0xa8;

    public XAssetType Type => XAssetType.Material;

    public MaterialInfo Info { get; init; } = new();
    public IReadOnlyList<MaterialStateBitsEntry> StateBitsEntries { get; init; } = [];
    public byte TextureCount { get; init; }
    public byte ConstantCount { get; init; }
    public byte StateBitsCount { get; init; }
    public byte StateFlags { get; init; }
    public byte CameraRegion { get; init; }
    public byte XStringCount { get; init; }
    public byte Pad43 { get; init; }
    public IReadOnlyList<ushort> InlineTechniqueSlotStateBits { get; init; } = [];
    public ushort Pad8E { get; init; }
    public XPointerReference RuntimeTechniqueSlotStateBitsPointer { get; init; }
    public IReadOnlyList<ushort> RuntimeTechniqueSlotStateBits { get; init; } = [];
    public XPointer<MaterialTechniqueSetAsset> TechniqueSetPointer { get; init; }
    public MaterialTechniqueSetAsset? TechniqueSet { get; init; }
    public XPointerReference TextureTablePointer { get; init; }
    public IReadOnlyList<MaterialTextureDef> Textures { get; init; } = [];
    public XPointerReference ConstantTablePointer { get; init; }
    public IReadOnlyList<MaterialConstantDef> Constants { get; init; } = [];
    public XPointerReference StateBitsPointer { get; init; }
    public IReadOnlyList<GfxStateBits> StateBits { get; init; } = [];
    public XPointerReference XStringTablePointer { get; init; }
    public IReadOnlyList<MaterialXStringEntry> XStrings { get; init; } = [];
}

public sealed class MaterialInfo
{
    public const int SerializedSize = 0x18;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public byte GameFlags { get; init; }
    public byte SortKey { get; init; }
    public byte TextureAtlasRowCount { get; init; }
    public byte TextureAtlasColumnCount { get; init; }
    public GfxDrawSurf DrawSurf { get; init; }
    public uint SurfaceTypeBits { get; init; }
    public ushort HashIndex { get; init; }
    public ushort Pad16 { get; init; }
}

public readonly record struct GfxDrawSurf(ulong Packed);

public readonly record struct MaterialStateBitsEntry(
    int TechniqueSlot,
    byte StateBitsIndex);

public sealed class MaterialTextureDef
{
    public const int SerializedSize = 0x0c;

    public uint NameHash { get; init; }
    public byte NameStart { get; init; }
    public byte NameEnd { get; init; }
    public byte SamplerState { get; init; }
    public byte Semantic { get; init; }
    public XPointerReference DataPointer { get; init; }
    public GfxImageAsset? Image { get; init; }
    public MaterialWater? Water { get; init; }
}

public sealed class MaterialConstantDef
{
    public const int SerializedSize = 0x20;

    public uint NameHash { get; init; }
    public IReadOnlyList<byte> NameBytes { get; init; } = [];
    public MaterialVec4 Literal { get; init; }
}

public readonly record struct MaterialVec4(float X, float Y, float Z, float W);

public sealed class GfxStateBits
{
    public const int SerializedSize = 0x08;

    public XPointerReference LoadBitsPointer { get; init; }
    public IReadOnlyList<uint> LoadBits { get; init; } = [];
    public uint Tail { get; init; }
}

public sealed class MaterialWater
{
    public const int SerializedSize = 0x48;

    public MaterialWaterWritable Writable { get; init; }
    public XPointerReference H0XPointer { get; init; }
    public XPointerReference H0YPointer { get; init; }
    public XPointerReference WTermPointer { get; init; }
    public int M { get; init; }
    public int N { get; init; }
    public float Lx { get; init; }
    public float Lz { get; init; }
    public float Gravity { get; init; }
    public float WindVelocity { get; init; }
    public MaterialVec2 WindDirection { get; init; }
    public float Amplitude { get; init; }
    public MaterialVec4 CodeConstant { get; init; }
    public XPointer<GfxImageAsset> ImagePointer { get; init; }
    public IReadOnlyList<float> H0X { get; init; } = [];
    public IReadOnlyList<float> H0Y { get; init; } = [];
    public IReadOnlyList<float> WTerm { get; init; } = [];
    public GfxImageAsset? Image { get; init; }
}

public readonly record struct MaterialWaterWritable(uint RawValue)
{
    public float FloatTime => BitConverter.Int32BitsToSingle(unchecked((int)RawValue));
}

public readonly record struct MaterialVec2(float X, float Y);

public sealed record MaterialXStringEntry(
    int Index,
    XString Pointer,
    string? Value);
