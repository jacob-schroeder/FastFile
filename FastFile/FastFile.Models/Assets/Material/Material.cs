using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Material;

public class Material() : BaseAsset(XAssetType.Material)
{
    public const int TECHNIQUE_COUNT = 37;

    public MaterialInfo Info { get; set; }
    public byte[] StateBitsEntry { get; set; } = new byte[TECHNIQUE_COUNT];
    public byte TextureCount { get; set; }
    public byte ConstantCount { get; set; }
    public byte StateBitsCount { get; set; }
    public byte StateFlags { get; set; }
    public byte CameraRegion { get; set; }
    public byte UnknownXStringCount { get; set; }
    public byte MaterialPadding { get; set; }
    public ushort[] Ushorts { get; set; } = new ushort[TECHNIQUE_COUNT];
    public byte[] UshortPadding { get; set; } = new byte[2];
    public XPointer<ushort[]> UshortArray { get; set; } // Direct
    public XPointer<MaterialTechniqueSet> TechniqueSet { get; set; } // Alias
    public XPointer<MaterialTextureDef[]> TextureTable { get; set; } // Direct
    public XPointer<MaterialConstantDef[]> ConstantTable { get; set; } // Direct
    public XPointer<GfxStateBits[]> StateBitTable { get; set; } // Direct
    public XPointer<XPointer<string>[]> UnknownXStringArray { get; set; } // Direct

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
    public XPointer<int[]> LoadBits { get; set; } // Direct
    public int Unknown { get; set; }
}

public class WaterWritable
{
    public float FloatTime { get; set; }
}

public class Water
{
    public WaterWritable Writable { get; set; }
    public XPointer<float[]> H0X { get; set; } // Direct
    public XPointer<float[]> H0Y { get; set; } // Direct
    public XPointer<float[]> WTerm { get; set; } // Direct
    public int M { get; set; }
    public int N { get; set; }
    public float Lx { get; set; }
    public float Lz { get; set; }
    public float Gravity { get; set; }
    public float Windvel { get; set; }
    public float[] Winddir { get; set; } = new float[2];
    public float Amplitude { get; set; }
    public float[] CodeConstant { get; set; } = new float[4];
    public XPointer<GfxImage> Image { get; set; } // Alias
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
    public XPointer<GfxImage> Image { get; set; } // Alias
    public XPointer<Water> Water { get; set; } // Direct
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
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
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
    public const int EBOOT_ROOT_SIZE = 0x50;
    public const int EBOOT_LOAD_DEF_POINTER_OFFSET = 0x28;
    public const int EBOOT_NAME_POINTER_OFFSET = 0x4C;

    public byte[] EbootRootPrefix { get; set; } = new byte[EBOOT_LOAD_DEF_POINTER_OFFSET];
    public XPointer<GfxImageLoadDef> LoadDef { get; set; } // Direct
    public byte[] EbootRootSuffix { get; set; } = new byte[EBOOT_NAME_POINTER_OFFSET - EBOOT_LOAD_DEF_POINTER_OFFSET - 4];
    public byte MapType { get; set; }
    public byte Semantic { get; set; }
    public byte Category { get; set; }
    public byte UseSrgbReads { get; set; }
    public byte[] Picmip { get; set; } = new byte[2];
    public byte NoPicmip { get; set; }
    public byte Track { get; set; }
    public int[] CardMemory { get; set; } = new int[2];
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Depth { get; set; }
    public byte DelayLoadPixels { get; set; }
    public byte[] Pad { get; set; } = new byte[3];

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
