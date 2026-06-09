namespace UI.Models;

public sealed class TechsetPassDisplayItem(
    string indexText,
    string vertexDeclaration,
    string vertexShader,
    string pixelShader,
    string argumentSummary,
    string arguments,
    string flagsAndPrecompiled)
{
    public string IndexText { get; } = indexText;

    public string VertexDeclaration { get; } = vertexDeclaration;

    public string VertexShader { get; } = vertexShader;

    public string PixelShader { get; } = pixelShader;

    public string ArgumentSummary { get; } = argumentSummary;

    public string Arguments { get; } = arguments;

    public string FlagsAndPrecompiled { get; } = flagsAndPrecompiled;
}
