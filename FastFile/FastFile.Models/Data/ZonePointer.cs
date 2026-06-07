namespace FastFile.Models.Data;

public class ZonePointer<T> : Pointer
{
    public T? Result { get; private set; }
    public bool IsResolved { get; private set; }

    public ZonePointer(int raw) : base(raw)
    {
    }

    public void SetResult(T? result)
    {
        Result = result;
        IsResolved = true;
    }
}

public sealed class DirectPointer<T> : ZonePointer<T>
{
    public DirectPointer(int raw) : base(raw)
    {
    }

    public override PointerResolutionKind DeclaredResolutionKind => PointerResolutionKind.Direct;
}

public sealed class AliasPointer<T> : ZonePointer<T>
{
    public AliasPointer(int raw) : base(raw)
    {
    }

    public override PointerResolutionKind DeclaredResolutionKind => PointerResolutionKind.Alias;
}
