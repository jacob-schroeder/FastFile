using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.TechniqueSet;

public struct MaterialPass
{
    public XPointer<int> VertexDecl; //MaterialVertexDeclaration
    public XPointer<int> VertexShader; //MaterialVertexShader
    public XPointer<int> PixelShader; //MaterialPixelShader
    public byte PerPrimArgCount;
    public byte PerObjArgCount;
    public byte StableArgCount;
    public byte CustomSamplerFlags;
    public byte PrecompiledIndex;
    public XPointer<int> Args; //MaterialShaderArgument
}