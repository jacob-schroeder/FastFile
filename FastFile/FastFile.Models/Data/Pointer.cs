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
    public int TargetOffset { get; private set; } = -1;
    public int TargetSpanOffset { get; private set; } = -1;
    public int TargetSpanLength { get; private set; } = -1;
    public int TargetSpanStreamBlockIndex { get; private set; } = -1;
    public int TargetSpanStreamOffset { get; private set; } = -1;
    public PointerResolutionKind ResolutionKind { get; private set; }
    public string FieldPath { get; private set; } = string.Empty;
    public int PointerFieldSourceOffset { get; private set; } = -1;
    public int PointerFieldSourceLength { get; private set; } = -1;
    public int PointerFieldStreamBlockIndex { get; private set; } = -1;
    public int PointerFieldStreamOffset { get; private set; } = -1;
    public int AliasCellStreamBlockIndex { get; private set; } = -1;
    public int AliasCellStreamOffset { get; private set; } = -1;
    public bool HasSourceSpan => SourceOffset >= 0 && SourceLength >= 0;
    public bool HasTargetOffset => TargetOffset >= 0;
    public bool HasTargetSpan => TargetSpanOffset >= 0 && TargetSpanLength >= 0;
    public bool HasTargetStreamSpan => TargetSpanStreamBlockIndex >= 0 && TargetSpanStreamOffset >= 0;
    public bool HasPointerFieldSourceSpan =>
        PointerFieldSourceOffset >= 0
        && PointerFieldSourceLength > 0
        && PointerFieldStreamBlockIndex >= 0
        && PointerFieldStreamOffset >= 0;
    public bool HasAliasCellStreamAddress => AliasCellStreamBlockIndex >= 0 && AliasCellStreamOffset >= 0;
    public bool IsInlineData => Kind is PointerKind.Inline or PointerKind.Insert;
    public bool CanMaterializeInline =>
        Kind is PointerKind.Inline
        || (Kind is PointerKind.Insert && ResolutionKind != PointerResolutionKind.Direct);
    public virtual PointerResolutionKind DeclaredResolutionKind => PointerResolutionKind.Unknown;

    public Pointer(int raw)
    {
        SetRaw(raw);
    }

    public void SetOffset(int address)
    {
        Offset = address;
        if (IsInlineData && SourceOffset < 0)
            SourceOffset = address;
    }

    public void SetStreamAddress(int streamBlockIndex, int offset)
    {
        StreamBlockIndex = streamBlockIndex;
        Offset = offset;
    }

    public void SetSourceSpan(int offset, int length)
    {
        SourceOffset = offset;
        SourceLength = length;
    }

    public void SetPointerFieldSourceSpan(
        int offset,
        int length,
        int streamBlockIndex,
        int streamOffset)
    {
        PointerFieldSourceOffset = offset;
        PointerFieldSourceLength = length;
        PointerFieldStreamBlockIndex = streamBlockIndex;
        PointerFieldStreamOffset = streamOffset;
    }

    public void SetResolutionKind(PointerResolutionKind kind, string? fieldPath = null)
    {
        if (kind != PointerResolutionKind.Unknown || ResolutionKind == PointerResolutionKind.Unknown)
            ResolutionKind = kind;

        if (!string.IsNullOrWhiteSpace(fieldPath))
            FieldPath = fieldPath;
    }

    public void SetAliasCellStreamAddress(int streamBlockIndex, int streamOffset)
    {
        AliasCellStreamBlockIndex = streamBlockIndex;
        AliasCellStreamOffset = streamOffset;
    }

    public void SetTargetOffset(int offset)
    {
        TargetOffset = offset;
    }

    public void SetTargetSpan(int offset, int length)
    {
        TargetSpanOffset = offset;
        TargetSpanLength = length;
    }

    public void SetTargetSpan(
        int offset,
        int length,
        int streamBlockIndex,
        int streamOffset)
    {
        SetTargetSpan(offset, length);
        TargetSpanStreamBlockIndex = streamBlockIndex;
        TargetSpanStreamOffset = streamOffset;
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

        if (raw is -1)
        {
            Kind = PointerKind.Inline;
            StreamBlockIndex = 0;
            Offset = SourceOffset >= 0 ? SourceOffset : 0;
            return;
        }

        if (raw is -2)
        {
            Kind = PointerKind.Insert;
            StreamBlockIndex = 0;
            Offset = SourceOffset >= 0 ? SourceOffset : 0;
            return;
        }

        var encoded = unchecked(raw - 1);
        Kind = PointerKind.Offset;
        StreamBlockIndex = (encoded >> 28) & StreamBlockMask;
        Offset = encoded & StreamOffsetMask;
    }

    public static int EncodeOffset(int streamBlockIndex, int offset)
    {
        return unchecked((((streamBlockIndex & StreamBlockMask) << 28) | (offset & StreamOffsetMask)) + 1);
    }
}
