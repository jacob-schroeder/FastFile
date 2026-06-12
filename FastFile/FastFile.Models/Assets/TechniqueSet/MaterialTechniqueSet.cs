using FastFile.Models.Zone;
using FastFile.Models.Data;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.TechniqueSet;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x9C)]
public class MaterialTechniqueSet() : BaseAsset(XAssetType.Techset)
{
    #if PS3
    private const int MAX_TECHNIQUES = 37;
    #elif XBOX
    private const int MAX_TECHNIQUES = 33;
    #elif PC
    private const int MAX_TECHNIQUES = 48;
    #endif
    
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04, Count = 4)]
    public byte[] HeaderBytes { get; set; } = new byte[4];
    public MaterialWorldVertexFormat WorldVertexFormat => (MaterialWorldVertexFormat)HeaderBytes[0];
    public bool HasBeenUploaded => HeaderBytes[1] != 0;

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<MaterialTechnique>[] Techniques { get; set; } = 
        new XPointer<MaterialTechnique>[MAX_TECHNIQUES]; // Direct

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"Techset 0x{Offset:X8}"
        : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class MaterialTechnique
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string?> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public ushort Flags { get; set; }

    [XField(Offset = 0x06)]
    public ushort PassCount { get; set; }
    public MaterialPass[] Passes { get; set; } = [];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public class MaterialPass
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<MaterialVertexDeclaration> VertexDecl { get; set; } // Direct

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<MaterialVertexShader> VertexShader { get; set; } // Alias

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<MaterialPixelShader> PixelShader { get; set; } // Alias

    [XField(Offset = 0x0C)]
    public byte PerPrimArgCount { get; set; }

    [XField(Offset = 0x0D)]
    public byte PerObjArgCount { get; set; }

    [XField(Offset = 0x0E)]
    public byte StableArgCount { get; set; }

    [XField(Offset = 0x0F)]
    public byte CustomSamplerFlags { get; set; }

    [XField(Offset = 0x10)]
    public byte PrecompiledIndex { get; set; }

    [XField(Offset = 0x11)]
    public byte[] Padding { get; set; } = new byte[3];

    [XField(Offset = 0x14)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(ArgCount))]
    public XPointer<MaterialShaderArgument[]> Args { get; set; } // Direct
    public int ArgCount => PerPrimArgCount + PerObjArgCount + StableArgCount;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class MaterialShaderArgument
{
    [XField(Offset = 0x00)]
    public MaterialShaderArgumentType Type { get; set; }

    [XField(Offset = 0x02)]
    public ushort Dest { get; set; }

    [XField(Offset = 0x04)]
    public MaterialArgumentDef Argument { get; set; } = new();
}

public enum MaterialShaderArgumentType : ushort
{
    MTL_ARG_MATERIAL_VERTEX_CONST = 0x0,
    MTL_ARG_LITERAL_VERTEX_CONST = 0x1,
    MTL_ARG_MATERIAL_PIXEL_SAMPLER = 0x2,
    MTL_ARG_CODE_VERTEX_CONST = 0x3,
    MTL_ARG_CODE_PIXEL_SAMPLER = 0x4,
    MTL_ARG_CODE_PIXEL_CONST = 0x5,
    MTL_ARG_MATERIAL_PIXEL_CONST = 0x6,
    MTL_ARG_LITERAL_PIXEL_CONST = 0x7,
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public class MaterialArgumentDef
{
    [XField(Offset = 0x00)]
    public int Raw { get; set; }
    public XPointer<float[]> LiteralConst { get; set; } // Direct
    public MaterialArgumentCodeConst CodeConst { get; set; } = new();
    public uint CodeSampler { get; set; }
    public uint NameHash { get; set; }
}

public class MaterialArgumentCodeConst
{
    public ushort Index { get; set; }
    public byte FirstRow { get; set; }
    public byte RowCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1C)]
public class MaterialVertexDeclaration
{
    public int Offset { get; set; }

    [XField(Offset = 0x00, Count = 0x1C)]
    public byte[] Raw { get; set; } = new byte[0x1C];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public class MaterialVertexShader() : BaseAsset(XAssetType.VertexShader)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public MaterialVertexShaderProgram Program { get; set; } = new();

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"VertexShader 0x{Offset:X8}"
        : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x18)]
public class MaterialPixelShader() : BaseAsset(XAssetType.PixelShader)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public MaterialPixelShaderProgram Program { get; set; } = new();

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"PixelShader 0x{Offset:X8}"
        : Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public class MaterialVertexShaderProgram
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        PayloadBlock = XFILE_BLOCK.XFILE_BLOCK_VERTEX,
        CountMember = nameof(DataSize))]
    public XPointer<byte[]> Data { get; set; } // Direct

    [XField(Offset = 0x04)]
    public int DataSize { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x14)]
public class MaterialPixelShaderProgram
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        PayloadBlock = XFILE_BLOCK.XFILE_BLOCK_VERTEX,
        CountMember = nameof(DataSize))]
    public XPointer<byte[]> Data { get; set; } // Direct

    [XField(Offset = 0x04)]
    public int DataSize { get; set; }

    [XField(Offset = 0x08)]
    public byte[] RootSuffix { get; set; } = new byte[0x0C];
}
