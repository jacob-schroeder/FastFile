namespace FastFile.Models.Data;

public class Pointer
{
    private const int StreamBlockMask = 0xF;
    private const int StreamOffsetMask = 0x0FFFFFFF;

    public int Raw { get; private set; }
    public PointerKind Kind { get; private set; }
    public int StreamBlockIndex { get; private set; }
    public int Offset { get; private set; }
    public int SourceOffset { get; private set; } = -1;
    public int SourceLength { get; private set; } = -1;
    public bool HasSourceSpan => SourceOffset >= 0 && SourceLength >= 0;

    public Pointer(int raw)
    {
        SetRaw(raw);
    }

    public void SetOffset(int address)
    {
        Offset = address;
        if (Kind == PointerKind.Inline && SourceOffset < 0)
            SourceOffset = address;
    }

    public void SetSourceSpan(int offset, int length)
    {
        SourceOffset = offset;
        SourceLength = length;
    }

    public void SetRaw(int raw)
    {
        Raw = raw;

        if (raw is 0)
        {
            Kind = PointerKind.Null;
            StreamBlockIndex = 0;
            Offset = 0;
            return;
        }

        if (raw is -1 or -2)
        {
            Kind = PointerKind.Inline;
            StreamBlockIndex = 0;
            Offset = SourceOffset >= 0 ? SourceOffset : 0;
            return;
        }

        Kind = PointerKind.Offset;
        StreamBlockIndex = (Raw >> 28) & StreamBlockMask;
        Offset = raw & StreamOffsetMask;
    }
}
