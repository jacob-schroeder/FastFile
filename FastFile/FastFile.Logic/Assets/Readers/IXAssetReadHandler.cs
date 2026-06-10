using System.Reflection;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public interface IXAssetReadHandler
{
    bool TryReadField(
        object owner,
        PropertyInfo property,
        XFieldAttribute field,
        IXAssetReaderContext context,
        out object? value);

    bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context);

    bool TryResolvePointers(
        object value,
        IXAssetReaderContext context);

    bool TryResolveField(
        object owner,
        object? value,
        IXAssetReaderContext context);
}
