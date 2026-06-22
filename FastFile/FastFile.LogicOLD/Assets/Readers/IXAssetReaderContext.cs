using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.LogicOLD.Assets.Readers;

public interface IXAssetReaderContext
{
    int SourcePosition { get; }

    XFILE_BLOCK ActiveStreamBlock { get; }

    int GetStreamPosition(XFILE_BLOCK block);

    XPointer<T> ReadPointer<T>(PointerResolutionKind resolutionKind);

    XPointer<T> ReinterpretPointer<T>(
        XPointer<object> pointer,
        PointerResolutionKind resolutionKind);

    byte[] ReadCurrentStreamBytes(int count);

    T ReadCurrentStreamObject<T>()
        where T : class, new();

    void MaterializeCStringPointer(XPointer<string?> pointer);

    void ResolveSndAliasCustomName(XPointer<string> pointer);

    void ResolveObjectPointers(object value);

    void ResolveChildPointers(object? value);

    void ResolvePointerProperty(object owner, string propertyName);

    void ResolvePointerValue(
        object value,
        XPointerFieldAttribute attribute,
        object owner);

    void ResolveCurrentStreamObjectPointer<T>(XPointer<T> pointer)
        where T : class, new();

    bool TryReadEmittedBytes(
        XBlockAddress address,
        int count,
        out byte[] value);

    void DeferEmittedBytes(
        XBlockAddress address,
        int count,
        Action<byte[]> onResolved);

    void WithStreamBlock(XFILE_BLOCK block, Action action);
}
