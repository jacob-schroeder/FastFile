using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public interface IXAssetReaderContext
{
    XPointer<T> ReadPointer<T>(PointerResolutionKind resolutionKind);

    XPointer<T> ReinterpretPointer<T>(
        XPointer<object> pointer,
        PointerResolutionKind resolutionKind);

    void MaterializeCStringPointer(XPointer<string?> pointer);

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
