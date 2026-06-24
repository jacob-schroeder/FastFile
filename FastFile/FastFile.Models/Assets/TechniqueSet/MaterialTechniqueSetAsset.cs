using FastFile.Models.Assets;
using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.TechniqueSet;

public sealed class MaterialTechniqueSetAsset : BaseAsset
{
    public const int SerializedSize = 0x9c;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public MaterialWorldVertexFormat WorldVertexFormat { get; init; }
    public byte[] Unknown05 { get; init; } = [];
    public IReadOnlyList<MaterialTechniqueSlot> TechniqueSlots { get; init; } = [];
}

public sealed record MaterialTechniqueSlot(
    int Index,
    XPointer<MaterialTechniqueAsset> Pointer,
    MaterialTechniqueAsset? Technique);

public sealed class MaterialTechniqueAsset
{
    public const int SerializedSize = 0x08;

    public int Offset { get; init; }
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public ushort Flags { get; init; }
    public ushort PassCount { get; init; }
    public IReadOnlyList<MaterialPassAsset> Passes { get; init; } = [];
}

public sealed class MaterialPassAsset
{
    public const int SerializedSize = 0x18;

    public int Offset { get; init; }
    public XPointer<MaterialVertexDeclarationAsset> VertexDeclPointer { get; init; }
    public XPointer<MaterialShaderAsset> VertexShaderPointer { get; init; }
    public XPointer<MaterialShaderAsset> PixelShaderPointer { get; init; }
    public byte PerPrimArgCount { get; init; }
    public byte PerObjArgCount { get; init; }
    public byte StableArgCount { get; init; }
    public byte CustomSamplerFlags { get; init; }
    public byte PrecompiledIndex { get; init; }
    public XPointer<MaterialShaderArgumentAsset[]> ArgsPointer { get; init; }
    public byte[]? VertexDeclBytes { get; set; }
    public MaterialShaderAsset? VertexShader { get; set; }
    public MaterialShaderAsset? PixelShader { get; set; }
    public IReadOnlyList<MaterialShaderArgumentAsset> Args { get; set; } = [];
}

public sealed class MaterialShaderAsset
{
    public int Offset { get; init; }
    public MaterialShaderKind Kind { get; init; }
    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<MaterialShaderBytecode> DataPointer { get; init; }
    public uint DataSize { get; init; }
    public byte[] ProgramBytes { get; init; } = [];
    public byte[]? Data { get; init; }
}

public enum MaterialShaderKind
{
    Vertex,
    Pixel
}

public sealed record MaterialShaderArgumentAsset(
    int Offset,
    ushort Type,
    ushort Dest,
    int ArgumentRaw,
    byte[]? LiteralFloat4Bytes);

public sealed class MaterialVertexDeclarationAsset;

public sealed class MaterialShaderBytecode;
