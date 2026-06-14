using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Material;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0xA8)]
public class Material() : BaseAsset(XAssetType.Material)
{
    public const int TECHNIQUE_COUNT = 37;
    public int TechniqueSlotCount => TECHNIQUE_COUNT;

    [XField(Offset = 0x00)]
    public MaterialInfo Info { get; set; }

    [XField(Offset = 0x18)]
    public byte[] StateBitsEntry { get; set; } = new byte[TECHNIQUE_COUNT];

    [XField(Offset = 0x3D)]
    public byte TextureCount { get; set; }

    [XField(Offset = 0x3E)]
    public byte ConstantCount { get; set; }

    [XField(Offset = 0x3F)]
    public byte StateBitsCount { get; set; }

    [XField(Offset = 0x40)]
    public byte StateFlags { get; set; }

    [XField(Offset = 0x41)]
    public byte CameraRegion { get; set; }

    [XField(Offset = 0x42)]
    public byte UnknownXStringCount { get; set; }

    [XField(Offset = 0x43)]
    public byte MaterialPadding { get; set; }

    [XField(Offset = 0x44)]
    public ushort[] Ushorts { get; set; } = new ushort[TECHNIQUE_COUNT];

    [XField(Offset = 0x8E)]
    public byte[] UshortPadding { get; set; } = new byte[2];

    [XField(Offset = 0x90)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.RUNTIME,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(TechniqueSlotCount))]
    public XPointer<ushort[]> UshortArray { get; set; } // Direct

    [XField(Offset = 0x94)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        OffsetIsAliasCell = true)]
    public XPointer<MaterialTechniqueSet> TechniqueSet { get; set; } // Alias

    [XField(Offset = 0x98)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 4,
        CountMember = nameof(TextureCount))]
    public XPointer<MaterialTextureDef[]> TextureTable { get; set; } // Direct

    [XField(Offset = 0x9C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 16,
        CountMember = nameof(ConstantCount))]
    public XPointer<MaterialConstantDef[]> ConstantTable { get; set; } // Direct

    [XField(Offset = 0xA0)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 4,
        CountMember = nameof(StateBitsCount))]
    public XPointer<GfxStateBits[]> StateBitTable { get; set; } // Direct

    [XField(Offset = 0xA4)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(UnknownXStringCount))]
    public XPointer<XPointer<string>[]> UnknownXStringArray { get; set; } // Direct

    public override string? GetDisplayName => Info?.Name ?? string.Empty;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x20)]
public class MaterialConstantDef
{
    [XField(Offset = 0x00)]
    public int NameHash { get; set; }

    [XField(Offset = 0x04, Count = 0x0C)]
    public byte[] NameBytes { get; set; } = new byte[0x0C];

    public string Name { get; set; } = string.Empty;

    [XField(Offset = 0x10)]
    public Vec4 Literal { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class GfxStateBits
{
    public int LoadBitsCount => 2;

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(LoadBitsCount))]
    public XPointer<int[]> LoadBits { get; set; } // Direct

    [XField(Offset = 0x04)]
    public int Unknown { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class WaterWritable
{
    [XField(Offset = 0x00)]
    public float FloatTime { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x48)]
public class Water
{
    public int ElementCount => checked(M * N);

    [XField(Offset = 0x00)]
    public WaterWritable Writable { get; set; }

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(ElementCount))]
    public XPointer<float[]> H0X { get; set; } // Direct

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(ElementCount))]
    public XPointer<float[]> H0Y { get; set; } // Direct

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(ElementCount))]
    public XPointer<float[]> WTerm { get; set; } // Direct

    [XField(Offset = 0x10)]
    public int M { get; set; }

    [XField(Offset = 0x14)]
    public int N { get; set; }

    [XField(Offset = 0x18)]
    public float Lx { get; set; }

    [XField(Offset = 0x1C)]
    public float Lz { get; set; }

    [XField(Offset = 0x20)]
    public float Gravity { get; set; }

    [XField(Offset = 0x24)]
    public float Windvel { get; set; }

    [XField(Offset = 0x28, Count = 2)]
    public float[] Winddir { get; set; } = new float[2];

    [XField(Offset = 0x30)]
    public float Amplitude { get; set; }

    [XField(Offset = 0x34, Count = 4)]
    public float[] CodeConstant { get; set; } = new float[4];

    [XField(Offset = 0x44)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true)]
    public XPointer<GfxImage> Image { get; set; } // Alias
}

public enum MaterialTextureSemantic : byte
{
    TS_2D = 0x0,
    TS_FUNCTION = 0x1,
    TS_COLOR_MAP = 0x2,
    TS_DETAIL_MAP = 0x3,
    TS_UNUSED_2 = 0x4,
    TS_NORMAL_MAP = 0x5,
    TS_UNUSED_3 = 0x6,
    TS_UNUSED_4 = 0x7,
    TS_SPECULAR_MAP = 0x8,
    TS_UNUSED_5 = 0x9,
    TS_UNUSED_6 = 0xA,
    TS_WATER_MAP = 0xB,
}

public enum GfxImageMapType : byte
{
    MAPTYPE_NONE = 0x0,
    MAPTYPE_INVALID1 = 0x1,
    MAPTYPE_1D = 0x2,
    MAPTYPE_2D = 0x3,
    MAPTYPE_3D = 0x4,
    MAPTYPE_CUBE = 0x5,
}

public enum GfxImageCategory : byte
{
    IMG_CATEGORY_UNKNOWN = 0x0,
    IMG_CATEGORY_AUTO_GENERATED = 0x1,
    IMG_CATEGORY_LIGHTMAP = 0x2,
    IMG_CATEGORY_LOAD_FROM_FILE = 0x3,
    IMG_CATEGORY_RAW = 0x4,
    IMG_CATEGORY_FIRST_UNMANAGED = 0x5,
    IMG_CATEGORY_WATER = 0x5,
    IMG_CATEGORY_RENDERTARGET = 0x6,
    IMG_CATEGORY_TEMP = 0x7,
}

public static class GfxImageFormats
{
    public const int D3dFormatA8R8G8B8 = 21;
    public const int D3dFormatX8R8G8B8 = 22;
    public const int D3dFormatR5G6B5 = 23;
    public const int D3dFormatA1R5G5B5 = 25;
    public const int D3dFormatA4R4G4B4 = 26;
    public const int D3dFormatA8 = 28;
    public const int D3dFormatL8 = 50;
    public const int D3dFormatA8L8 = 51;
    public const int D3dFormatDxt1Le = 0x31545844;
    public const int D3dFormatDxt3Le = 0x33545844;
    public const int D3dFormatDxt5Le = 0x35545844;
    public const int D3dFormatDxt1Be = 0x44585431;
    public const int D3dFormatDxt3Be = 0x44585433;
    public const int D3dFormatDxt5Be = 0x44585435;
    public const int GcmFormatA8R8G8B8 = 0x85;
    public const int GcmFormatDxt1 = 0x86;
    public const int GcmFormatDxt23 = 0x87;
    public const int GcmFormatDxt45 = 0x88;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class MaterialTextureDefInfo
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<object> DataPtr { get; set; }

    public int Raw => DataPtr?.Raw ?? 0;
    public XPointer<GfxImage> Image { get; set; } // Alias
    public XPointer<Water> Water { get; set; } // Direct
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public class MaterialTextureDef
{
    [XField(Offset = 0x00)]
    public uint NameHash { get; set; }

    [XField(Offset = 0x04)]
    public byte NameStart { get; set; }

    [XField(Offset = 0x05)]
    public byte NameEnd { get; set; }

    [XField(Offset = 0x06)]
    public byte SampleState { get; set; }

    [XField(Offset = 0x07)]
    public MaterialTextureSemantic Semantic { get; set; }

    public byte IsMatureContent { get; set; }
    public byte[] Pad { get; set; } = new byte[3];

    [XField(Offset = 0x08)]
    public MaterialTextureDefInfo Info { get; set; }
}

public class GfxDrawSurfFields
{
    public ulong Packed { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class GfxDrawSurf
{
    public GfxDrawSurfFields Fields { get; set; }
    [XField(Offset = 0x00)]
    public ulong Packed { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public class MaterialInfo
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public byte GameFlags { get; set; }

    [XField(Offset = 0x05)]
    public byte SortKey { get; set; }

    [XField(Offset = 0x06)]
    public byte TextureAtlasRowCount { get; set; }

    [XField(Offset = 0x07)]
    public byte TextureAtlasColumnCount { get; set; }

    [XField(Offset = 0x08)]
    public GfxDrawSurf DrawSurf { get; set; }

    [XField(Offset = 0x10)]
    public int SurfaceTypeBits { get; set; }

    [XField(Offset = 0x14)]
    public int Padding { get; set; }
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x50)]
public class GfxImage() : BaseAsset(XAssetType.Image)
{
    public const int EBOOT_ROOT_SIZE = 0x50;
    public const int EBOOT_LOAD_DEF_POINTER_OFFSET = 0x28;
    public const int EBOOT_NAME_POINTER_OFFSET = 0x4C;
    public const int EBOOT_PAYLOAD_ALIGNMENT = 0x80;

    [XField(Offset = 0x00)]
    public byte[] EbootRootPrefix { get; set; } = new byte[EBOOT_LOAD_DEF_POINTER_OFFSET];

    [XField(Offset = EBOOT_LOAD_DEF_POINTER_OFFSET)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<GfxImageLoadDef> LoadDef { get; set; } // PS3 payload cell, surfaced as a semantic overlay

    [XField(Offset = EBOOT_LOAD_DEF_POINTER_OFFSET + 4)]
    public byte[] EbootRootSuffix { get; set; } = new byte[EBOOT_NAME_POINTER_OFFSET - EBOOT_LOAD_DEF_POINTER_OFFSET - 4];

    public byte FormatByte { get; set; }
    public byte LevelCount { get; set; }
    public byte TextureControl { get; set; }
    public byte MultiFaceControl { get; set; }
    public int TextureFlags { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ushort Depth { get; set; }
    public byte[] PlatformTextureHeader { get; set; } = new byte[0x0A];
    public GfxImageMapType MapType { get; set; }
    public MaterialTextureSemantic Semantic { get; set; }
    public GfxImageCategory Category { get; set; }
    public byte UseSrgbReads { get; set; }
    public byte[] Picmip { get; set; } = new byte[2];
    public byte NoPicmip { get; set; }
    public byte Track { get; set; }
    public int[] PlatformControlWords { get; set; } = new int[2];
    public byte[] PlatformTextureTail { get; set; } = new byte[0x1C];
    public byte DelayLoadPixels { get; set; }
    public byte[] NamePadding { get; set; } = new byte[3];

    [XField(Offset = EBOOT_NAME_POINTER_OFFSET)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    public override string? GetDisplayName => Name;
}

public class GfxImageLoadDef
{
    public byte LevelCount { get; set; }
    public byte[] Reserved { get; set; } = new byte[3];
    public int Flags { get; set; }
    public int Format { get; set; }
    public int ResourceSize { get; set; }
    public byte[] Data { get; set; } = [];
}
