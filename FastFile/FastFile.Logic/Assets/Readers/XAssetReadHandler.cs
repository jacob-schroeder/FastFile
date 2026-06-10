using System.Reflection;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public abstract class XAssetReadHandler : IXAssetReadHandler
{
    public virtual bool TryReadField(
        object owner,
        PropertyInfo property,
        XFieldAttribute field,
        IXAssetReaderContext context,
        out object? value)
    {
        value = null;
        return false;
    }

    public virtual bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        return false;
    }

    public virtual bool TryResolvePointers(
        object value,
        IXAssetReaderContext context)
    {
        return false;
    }

    public virtual bool TryResolveField(
        object owner,
        object? value,
        IXAssetReaderContext context)
    {
        return false;
    }
}
