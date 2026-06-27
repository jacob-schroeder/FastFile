namespace FastFile.Emitters;

public sealed class XEmitDiagnostics
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public void Trace(string message)
    {
        _messages.Add(message);
    }
}
