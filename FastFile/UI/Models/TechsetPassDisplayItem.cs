namespace UI.Models;

public sealed class TechsetPassDisplayItem(
    string indexText,
    string vertexDeclaration,
    string vertexShader,
    string pixelShader,
    string argumentSummary,
    string arguments,
    string flagsAndPrecompiled,
    BlockStreamNavigationTarget? vertexDeclarationNavigationTarget = null,
    BlockStreamNavigationTarget? vertexShaderNavigationTarget = null,
    BlockStreamNavigationTarget? pixelShaderNavigationTarget = null,
    BlockStreamNavigationTarget? argumentsNavigationTarget = null)
{
    public string IndexText { get; } = indexText;

    public string VertexDeclaration { get; } = vertexDeclaration;

    public BlockStreamNavigationTarget? VertexDeclarationNavigationTarget { get; } = vertexDeclarationNavigationTarget;

    public string VertexDeclarationNavigationValue => VertexDeclarationNavigationTarget?.ReplaceOffsetLabel(VertexDeclaration) ?? VertexDeclaration;

    public bool HasVertexDeclarationNavigationTarget => VertexDeclarationNavigationTarget is not null;

    public bool HasNoVertexDeclarationNavigationTarget => VertexDeclarationNavigationTarget is null;

    public string VertexShader { get; } = vertexShader;

    public BlockStreamNavigationTarget? VertexShaderNavigationTarget { get; } = vertexShaderNavigationTarget;

    public string VertexShaderNavigationValue => VertexShaderNavigationTarget?.ReplaceOffsetLabel(VertexShader) ?? VertexShader;

    public bool HasVertexShaderNavigationTarget => VertexShaderNavigationTarget is not null;

    public bool HasNoVertexShaderNavigationTarget => VertexShaderNavigationTarget is null;

    public string PixelShader { get; } = pixelShader;

    public BlockStreamNavigationTarget? PixelShaderNavigationTarget { get; } = pixelShaderNavigationTarget;

    public string PixelShaderNavigationValue => PixelShaderNavigationTarget?.ReplaceOffsetLabel(PixelShader) ?? PixelShader;

    public bool HasPixelShaderNavigationTarget => PixelShaderNavigationTarget is not null;

    public bool HasNoPixelShaderNavigationTarget => PixelShaderNavigationTarget is null;

    public string ArgumentSummary { get; } = argumentSummary;

    public string Arguments { get; } = arguments;

    public BlockStreamNavigationTarget? ArgumentsNavigationTarget { get; } = argumentsNavigationTarget;

    public string ArgumentsNavigationValue => ArgumentsNavigationTarget?.ReplaceOffsetLabel(Arguments) ?? Arguments;

    public bool HasArgumentsNavigationTarget => ArgumentsNavigationTarget is not null;

    public bool HasNoArgumentsNavigationTarget => ArgumentsNavigationTarget is null;

    public string FlagsAndPrecompiled { get; } = flagsAndPrecompiled;
}
