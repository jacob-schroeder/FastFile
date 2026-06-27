using FastFile.Models.Zone;

namespace FastFile.Models.Codecs;

public sealed record XCodecRecipe(
    string Name,
    IReadOnlyList<XCodecOperation> Operations,
    string Evidence)
{
    public static XCodecRecipe FromFields(
        string name,
        IReadOnlyList<XFieldContract> fields,
        string evidence)
    {
        return new XCodecRecipe(
            name,
            fields.Select(XCodecOps.Field).ToArray(),
            evidence);
    }
}

public abstract record XCodecOperation(
    string Name,
    XCodecOperationKind Kind,
    string Evidence);

public sealed record XFieldCodecOperation(
    XFieldContract Field)
    : XCodecOperation(Field.Name, XCodecOperationKind.Field, Field.Evidence);

public sealed record XStreamStructCodecOperation(
    string Name,
    int Size,
    XFileBlockType? DestinationBlock,
    int Alignment,
    string Evidence)
    : XCodecOperation(Name, XCodecOperationKind.StreamStruct, Evidence);

public sealed record XAlignCodecOperation(
    int Alignment,
    string Evidence)
    : XCodecOperation($"align{Alignment}", XCodecOperationKind.Align, Evidence);

public sealed record XBlockCodecOperation(
    XBlockCodecOperationAction Action,
    XFileBlockType Block,
    string Evidence)
    : XCodecOperation($"{Action}{Block}", Action == XBlockCodecOperationAction.Push ? XCodecOperationKind.PushBlock : XCodecOperationKind.PopBlock, Evidence);

public sealed record XCustomCodecOperation(
    string Name,
    string Evidence)
    : XCodecOperation(Name, XCodecOperationKind.Custom, Evidence);

public static class XCodecOps
{
    public static XFieldCodecOperation Field(XFieldContract field)
    {
        return new XFieldCodecOperation(field);
    }

    public static XStreamStructCodecOperation StreamStruct(
        string name,
        int size,
        XFileBlockType? destinationBlock,
        int alignment,
        string evidence)
    {
        return new XStreamStructCodecOperation(name, size, destinationBlock, alignment, evidence);
    }

    public static XAlignCodecOperation Align(int alignment, string evidence)
    {
        return new XAlignCodecOperation(alignment, evidence);
    }

    public static XBlockCodecOperation PushBlock(XFileBlockType block, string evidence)
    {
        return new XBlockCodecOperation(XBlockCodecOperationAction.Push, block, evidence);
    }

    public static XBlockCodecOperation PopBlock(XFileBlockType block, string evidence)
    {
        return new XBlockCodecOperation(XBlockCodecOperationAction.Pop, block, evidence);
    }

    public static XCustomCodecOperation Custom(string name, string evidence)
    {
        return new XCustomCodecOperation(name, evidence);
    }
}

public enum XCodecOperationKind
{
    StreamStruct,
    Field,
    Align,
    PushBlock,
    PopBlock,
    Custom
}

public enum XBlockCodecOperationAction
{
    Push,
    Pop
}
