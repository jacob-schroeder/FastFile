using FastFile.Models.Zone;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.TechniqueSet;

public class MaterialTechniqueSet() : BaseAsset(XAssetType.Techset)
{
    #if PS3
    private const int MAX_TECHNIQUES = 37;
    #elif XBOX
    private const int MAX_TECHNIQUES = 33;
    #elif PC
    private const int MAX_TECHNIQUES = 48;
    #endif
    
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    
    public MaterialWorldVertexFormat WorldVertexFormat { get; set; }
    public bool HasBeenUploaded { get; set; }
    public byte[] Unused { get; set; } = new byte[2];

    public ZonePointer<MaterialTechnique>[] Techniques { get; set; } = new ZonePointer<MaterialTechnique>[MAX_TECHNIQUES];

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"Techset 0x{Offset:X8}"
        : Name;
}

public class MaterialTechnique
{
    public int Offset { get; set; }
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public ushort Flags { get; set; }
    public ushort PassCount { get; set; }
    public MaterialPass[] Passes { get; set; } = [];
}

public class MaterialPass
{
    public int Offset { get; set; }
    public ZonePointer<MaterialVertexDeclaration> VertexDecl { get; set; } = new(0);
    public ZonePointer<MaterialVertexShader> VertexShader { get; set; } = new(0);
    public ZonePointer<MaterialPixelShader> PixelShader { get; set; } = new(0);
    public byte PerPrimArgCount { get; set; }
    public byte PerObjArgCount { get; set; }
    public byte StableArgCount { get; set; }
    public byte CustomSamplerFlags { get; set; }
    public byte PrecompiledIndex { get; set; }
    public byte[] Padding { get; set; } = new byte[3];
    public ZonePointer<MaterialShaderArgument[]> Args { get; set; } = new(0);
    public int ArgCount => PerPrimArgCount + PerObjArgCount + StableArgCount;
}

public class MaterialShaderArgument
{
    public MaterialShaderArgumentType Type { get; set; }
    public ushort Dest { get; set; }
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

public class MaterialArgumentDef
{
    public int Raw { get; set; }
    public ZonePointer<float[]> LiteralConst { get; set; } = new(0);
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

public class MaterialVertexDeclaration
{
    public int Offset { get; set; }
    public byte[] Raw { get; set; } = new byte[0x1C];
}

public class MaterialVertexShader() : BaseAsset(XAssetType.VertexShader)
{
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public MaterialVertexShaderProgram Program { get; set; } = new();

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"VertexShader 0x{Offset:X8}"
        : Name;
}

public class MaterialPixelShader() : BaseAsset(XAssetType.PixelShader)
{
    public ZonePointer<string> NamePtr { get; set; } = new(0);
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public MaterialPixelShaderProgram Program { get; set; } = new();

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(Name)
        ? $"PixelShader 0x{Offset:X8}"
        : Name;
}

public class MaterialVertexShaderProgram
{
    public int Offset { get; set; }
    public ZonePointer<byte[]> Data { get; set; } = new(0);
    public int DataSize { get; set; }
}

public class MaterialPixelShaderProgram
{
    public int Offset { get; set; }
    public ZonePointer<byte[]> Data { get; set; } = new(0);
    public int DataSize { get; set; }
    public byte[] RootSuffix { get; set; } = new byte[0x0C];
}
