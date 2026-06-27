using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Codecs;

public interface IXCodecContract
{
    string Name { get; }
    int SerializedSize { get; }
    IReadOnlyList<XFieldContract> Fields { get; }
    XCodecRecipe Recipe { get; }
    XCodecReadiness Readiness { get; }
    string Evidence { get; }
}

public interface IXAssetCodecContract : IXCodecContract
{
    XAssetType AssetType { get; }
    XStructCodecContract RootStruct { get; }
}

public interface IXCodecContractProvider
{
    IXCodecContract Contract { get; }
}

public interface IXCodecContractProvider<out TContract>
    where TContract : IXCodecContract
{
    TContract Contract { get; }
}

public sealed class XStructCodecContract : IXCodecContract
{
    public XStructCodecContract(
        string name,
        int serializedSize,
        IReadOnlyList<XFieldContract> fields,
        string evidence,
        XCodecReadiness readiness = XCodecReadiness.LoaderProven,
        XCodecRecipe? recipe = null)
    {
        Name = name;
        SerializedSize = serializedSize;
        Fields = fields;
        Evidence = evidence;
        Readiness = readiness;
        Recipe = recipe ?? XCodecRecipe.FromFields(name, fields, evidence);
    }

    public string Name { get; }
    public int SerializedSize { get; }
    public IReadOnlyList<XFieldContract> Fields { get; }
    public XCodecRecipe Recipe { get; }
    public XCodecReadiness Readiness { get; }
    public string Evidence { get; }

    public XFieldContract GetField(string name)
    {
        return Fields.First(field => field.Name == name);
    }

    public XPointerFieldContract GetPointerField(string name)
    {
        return (XPointerFieldContract)GetField(name);
    }
}

public sealed class XAssetCodecContract : IXAssetCodecContract
{
    public XAssetCodecContract(
        XAssetType assetType,
        XStructCodecContract rootStruct,
        string evidence,
        XCodecReadiness readiness = XCodecReadiness.LoaderProven,
        XCodecRecipe? recipe = null)
    {
        AssetType = assetType;
        RootStruct = rootStruct;
        Name = assetType.ToString();
        SerializedSize = rootStruct.SerializedSize;
        Fields = rootStruct.Fields;
        Evidence = evidence;
        Readiness = readiness;
        Recipe = recipe ?? rootStruct.Recipe;
    }

    public XAssetType AssetType { get; }
    public XStructCodecContract RootStruct { get; }
    public string Name { get; }
    public int SerializedSize { get; }
    public IReadOnlyList<XFieldContract> Fields { get; }
    public XCodecRecipe Recipe { get; }
    public XCodecReadiness Readiness { get; }
    public string Evidence { get; }
}

public abstract record XFieldContract(
    string Name,
    int Offset,
    int Size,
    string Evidence)
{
    public int EndOffset => Offset + Size;
}

public sealed record XScalarFieldContract(
    string Name,
    int Offset,
    int Size,
    string FieldType,
    string Evidence)
    : XFieldContract(Name, Offset, Size, Evidence);

public sealed record XStructFieldContract(
    string Name,
    int Offset,
    int Size,
    string StructName,
    string Evidence)
    : XFieldContract(Name, Offset, Size, Evidence);

public sealed record XArrayFieldContract(
    string Name,
    int Offset,
    int Size,
    string ElementType,
    int ElementCount,
    int ElementSize,
    string Evidence)
    : XFieldContract(Name, Offset, Size, Evidence);

public sealed record XPointerFieldContract(
    string Name,
    int Offset,
    string Target,
    XPointerResolutionMode ResolutionMode,
    XPointerSourceSemantics SourceSemantics,
    string Evidence,
    int InlineAlignment = 0,
    XFileBlockType? InlineBlock = null,
    XAssetType? TargetAssetType = null)
    : XFieldContract(Name, Offset, 4, Evidence);

public enum XPointerSourceSemantics
{
    ReferenceOnly,
    NullableReferenceOrInline,
    RequiredInline,
    NullableReferenceInlineOrInsert
}

public enum XCodecReadiness
{
    Open,
    LoaderProven,
    EmitterReady,
    CompilerReady
}
