namespace FastFile.Emitters;

public sealed class XEmitContext
{
    public XEmitContext()
        : this(new XSourceWriter(), new XProjectedBlockState(), new XEmitDiagnostics())
    {
    }

    public XEmitContext(
        XSourceWriter source,
        XProjectedBlockState blocks,
        XEmitDiagnostics diagnostics)
    {
        Source = source;
        Blocks = blocks;
        Diagnostics = diagnostics;
    }

    public XSourceWriter Source { get; }
    public XProjectedBlockState Blocks { get; }
    public XEmitDiagnostics Diagnostics { get; }
}
