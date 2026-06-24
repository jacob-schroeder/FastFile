namespace FastFile.Runtime.Diagnostics;

public sealed class LoadDiagnostics
{
    private readonly List<string> _warnings = new();
    private readonly List<string> _trace = new();

    public IReadOnlyList<string> Warnings => _warnings;
    public IReadOnlyList<string> TraceLines => _trace;

    public void Warn(string message)
    {
        _warnings.Add(message);
    }

    public void Trace(string message)
    {
        _trace.Add(message);
    }
}
