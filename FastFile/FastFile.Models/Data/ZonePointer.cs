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