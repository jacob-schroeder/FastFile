using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Menu;

public sealed class StaticDvar
{
    public const int SerializedSize = 0x08;

    // Runtime cache slot. PS3 loader reads the word but does not load string data here;
    // default_mp helper 0x0022a828 fills it from DvarName on first STATICDVAR* use.
    public XPointer<DvarRuntimeHandle> Dvar { get; init; }

    public XPointer<string> DvarName { get; init; }
    public string? DvarNameString { get; set; }
}

public sealed class DvarRuntimeHandle;

public sealed class StaticDvarList
{
    public const int SerializedSize = 0x08;

    public int NumStaticDvars { get; init; }
    public XPointer<XPointer<StaticDvar>[]> StaticDvars { get; init; }
    public IReadOnlyList<StaticDvarReference> LoadedStaticDvars { get; set; } = [];
}

public sealed class UIFunctionList
{
    public const int SerializedSize = 0x08;

    public int TotalFunctions { get; init; }
    public XPointer<XPointer<Statement>[]> Functions { get; init; }
    public IReadOnlyList<StatementReference> LoadedFunctions { get; set; } = [];
}

public sealed class StringList
{
    public const int SerializedSize = 0x08;

    public int TotalStrings { get; init; }
    public XPointer<XPointer<string>[]> Strings { get; init; }
    public IReadOnlyList<XStringReference> LoadedStrings { get; set; } = [];
}

public sealed record StatementReference(
    int Index,
    XPointer<Statement> Pointer,
    Statement? Statement);

public sealed record StaticDvarReference(
    int Index,
    XPointer<StaticDvar> Pointer,
    StaticDvar? StaticDvar);

public sealed record XStringReference(
    int Index,
    XString Pointer,
    string? Value);
