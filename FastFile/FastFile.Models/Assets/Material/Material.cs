using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Material;

public class Material() : BaseAsset(XAssetType.Material)
{
#if PS3
    public const int TECHNIQUE_COUNT = 38;
#elif XBOX
    public const int TECHNIQUE_COUNT = 33;
#else
    public const int TECHNIQUE_COUNT = 48;
#endif

    public MaterialInfo Info { get; set; }
    public byte[] StateBitsEntry { get; set; } = new byte[TECHNIQUE_COUNT];
    public byte TextureCount { get; set; }
    public byte ConstantCount { get; set; }
    public byte StateBitsCount { get; set; }
    public byte StateFlags { get; set; }
    public byte CameraRegion { get; set; }
#if PS3
    public ushort[] Ushorts { get; set; } = new ushort[TECHNIQUE_COUNT];
    public ZonePointer<ushort[]> UshortArray { get; set; }
#endif
    public ZonePointer<MaterialTechniqueSet> TechniqueSet { get; set; }
    public ZonePointer<MaterialTextureDef[]> TextureTable { get; set; }
    public ZonePointer<MaterialConstantDef[]> ConstantTable { get; set; }
    public ZonePointer<GfxStateBits[]> StateBitTable { get; set; }
    public ZonePointer<ZonePointer<string>[]> UnknownXStringArray { get; set; }

    public override string? GetDisplayName => Info?.Name ?? string.Empty;
}

public class MaterialConstantDef
{
    public int NameHash { get; set; }
    public string Name { get; set; } = string.Empty;
    public Vec4 Literal { get; set; }
}

public class GfxStateBits
{
#if XBOX
    public int[] LoadBits { get; set; } = new int[2];
#elif PS3
    public ZonePointer<int[]> LoadBits { get; set; }
    public int Unknown { get; set; }
#endif
}

public class WaterWritable
{
    public float FloatTime { get; set; }
}

public class Water
{
    public WaterWritable Writable { get; set; }
    public ZonePointer<float[]> H0X { get; set; }
    public ZonePointer<float[]> H0Y { get; set; }
    public ZonePointer<float[]> WTerm { get; set; }
    public int M { get; set; }
    public int N { get; set; }
    public float Lx { get; set; }
    public float Lz { get; set; }
    public float Gravity { get; set; }
    public float Windvel { get; set; }
    public float[] Winddir { get; set; } = new float[2];
    public float Amplitude { get; set; }
    public float[] CodeConstant { get; set; } = new float[4];
    public ZonePointer<GfxImage> Image { get; set; }
}

public enum MaterialTextureSemantic : byte
{
    TS_2D = 0x0,
    TS_FUNCTION = 0x1,
    TS_COLOR_MAP = 0x2,
    TS_UNUSED_1 = 0x3,
    TS_UNUSED_2 = 0x4,
    TS_NORMAL_MAP = 0x5,
    TS_UNUSED_3 = 0x6,
    TS_UNUSED_4 = 0x7,
    TS_SPECULAR_MAP = 0x8,
    TS_UNUSED_5 = 0x9,
    TS_UNUSED_6 = 0xA,
    TS_WATER_MAP = 0xB,
}

public class MaterialTextureDefInfo
{
    public int Raw { get; set; }
    public ZonePointer<GfxImage> Image { get; set; }
    public ZonePointer<Water> Water { get; set; }
}

public class MaterialTextureDef
{
    public uint NameHash { get; set; }
    public byte NameStart { get; set; }
    public byte NameEnd { get; set; }
    public byte SampleState { get; set; }
    public MaterialTextureSemantic Semantic { get; set; }
    public byte IsMatureContent { get; set; }
    public byte[] Pad { get; set; } = new byte[3];
    public MaterialTextureDefInfo Info { get; set; }
}

public class GfxDrawSurfFields
{
    public ulong Packed { get; set; }
}

public class GfxDrawSurf
{
    public GfxDrawSurfFields Fields { get; set; }
    public ulong Packed { get; set; }
}

public class MaterialInfo
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public byte GameFlags { get; set; }
    public byte SortKey { get; set; }
    public byte TextureAtlasRowCount { get; set; }
    public byte TextureAtlasColumnCount { get; set; }
    public GfxDrawSurf DrawSurf { get; set; }
    public int SurfaceTypeBits { get; set; }
    public int Padding { get; set; }
}

public class GfxImage() : BaseAsset(XAssetType.Image)
{
    public ZonePointer<GfxImageLoadDef> LoadDef { get; set; } = new(0);
    public byte MapType { get; set; }
    public byte Semantic { get; set; }
    public byte Category { get; set; }
    public byte UseSrgbReads { get; set; }
    public byte[] Picmip { get; set; } = new byte[2];
    public byte NoPicmip { get; set; }
    public byte Track { get; set; }
    public int[] CardMemory { get; set; } = new int[2];
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Depth { get; set; }
    public byte DelayLoadPixels { get; set; }

    public override string? GetDisplayName => Name;
}

public class GfxImageLoadDef
{
    public byte LevelCount { get; set; }
    public byte[] Pad { get; set; } = new byte[3];
    public int Flags { get; set; }
    public int Format { get; set; }
    public int ResourceSize { get; set; }
    public byte[] Data { get; set; } = [];
}
