using FastFile.Models.Pointers.Enums;

namespace FastFile.Models.Pointers.Resolvers;

public interface IXPointerResolver
{
    bool TryGetValue<T>(XPointer<T> pointer, out T? value);
    T ReadInline<T>(XPointer<T> pointer);
    void Materialize<T>(XPointer<T> pointer, PointerType type);
}