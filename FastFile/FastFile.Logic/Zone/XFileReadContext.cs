using FastFile.Models.Data;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

internal delegate T XFileValueReader<T>(ref XFileReadContext context);

internal delegate void XFilePointerResolver<T>(
    ref XFileReadContext context,
    ZonePointer<T> pointer);

internal interface IQueuedXFilePointerResolver
{
    string Name { get; }
    string FieldPath { get; }
    int Raw { get; }
    XFILE_BLOCK? MaterializationBlock { get; }
    void Resolve(ref XFileReadContext context);
}

internal readonly record struct XFileResolverScope(int InlineCount, int DeferredCount);

internal sealed class QueuedXFilePointerResolver<T>(
    ZonePointer<T> pointer,
    XFilePointerResolver<T> resolver,
    XFILE_BLOCK? materializationBlock = null,
    XFileStreamAlignment? streamAlignment = null) : IQueuedXFilePointerResolver
{
    public string Name => typeof(T).Name;
    public string FieldPath => pointer.FieldPath;
    public int Raw => pointer.Raw;
    public XFILE_BLOCK? MaterializationBlock => materializationBlock;

    public void Resolve(ref XFileReadContext context)
    {
        if (materializationBlock is not { } block)
        {
            ResolveCore(ref context);
            return;
        }

        context.PushStreamBlock(block);
        try
        {
            ResolveCore(ref context);
        }
        finally
        {
            context.PopStreamBlock();
        }
    }

    private void ResolveCore(ref XFileReadContext context)
    {
        if (streamAlignment is { } alignment)
            context.AlignStreamOnly(alignment);

        var start = context.Position;
        context.PrepareInlinePointerMaterialization(pointer);
        var streamStart = context.GetActiveStreamAddress();
        Memory.ResolvePointer(pointer, context.Position);
        context.ResolvePointerStreamAddress(pointer, streamStart);

        try
        {
            resolver(ref context, pointer);
            var length = context.Position - start;
            pointer.SetSourceSpan(start, length);
            context.RegisterMaterializedPointer(pointer, typeof(T), start, length, streamStart);
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw new InvalidDataException(
                $"Failed to resolve inline pointer for {typeof(T).Name}; raw=0x{pointer.Raw:X8}, offset=0x{pointer.Offset:X8}, current zone offset=0x{context.Position:X8} ({context.Position:N0}).",
                ex);
        }
    }
}

internal static class XFileReadTrace
{
    private static readonly bool TraceResolve = IsTraceEnabled("FASTFILE_TRACE_RESOLVE");
    private static readonly int TraceResolveLimit = GetInt("FASTFILE_TRACE_RESOLVE_LIMIT", int.MaxValue);
    private static readonly int TraceResolveStart = GetOffset("FASTFILE_TRACE_RESOLVE_START", 0);
    private static readonly int TraceResolveEnd = GetOffset("FASTFILE_TRACE_RESOLVE_END", int.MaxValue);
    private static int _traceResolveCount;

    public static void TraceResolver(
        string phase,
        IQueuedXFilePointerResolver resolver,
        int zoneOffset,
        XFileBlockAddress streamAddress,
        int assetIndex,
        XAssetType assetType,
        int queueIndex,
        int olderSiblingCount,
        int deferredCount,
        int zoneLength)
    {
        var zoneEnd = zoneOffset + Math.Max(0, zoneLength);
        var overlapsWindow = phase == "end"
            ? zoneOffset <= TraceResolveEnd && zoneEnd >= TraceResolveStart
            : zoneOffset >= TraceResolveStart && zoneOffset <= TraceResolveEnd;

        if (!TraceResolve
            || !overlapsWindow
            || _traceResolveCount >= TraceResolveLimit)
        {
            return;
        }

        Interlocked.Increment(ref _traceResolveCount);
        var block = resolver.MaterializationBlock is { } materializationBlock
            ? $" materialize=b{(int)materializationBlock}"
            : string.Empty;
        var field = string.IsNullOrWhiteSpace(resolver.FieldPath)
            ? string.Empty
            : $" field={resolver.FieldPath}";

        Console.Error.WriteLine(
            $"[resolve-trace] {phase} asset[{assetIndex:D5}:{assetType}] q={queueIndex} "
            + $"src=0x{zoneOffset:X8}/0x{zoneLength:X} stream=b{streamAddress.BlockIndex}:0x{streamAddress.Offset:X8} "
            + $"type={resolver.Name} raw=0x{resolver.Raw:X8}{block}{field} "
            + $"older={olderSiblingCount} deferred={deferredCount}");
    }

    private static bool IsTraceEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }

    private static int GetInt(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value >= 0
            ? value
            : fallback;
    }

    private static int GetOffset(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
        {
            return hex;
        }

        return int.TryParse(value, out var decimalValue)
            ? decimalValue
            : fallback;
    }
}

internal ref struct XFileReadContext
{
    private readonly ReadOnlySpan<byte> _span;
    private readonly XFileStreamLayout? _streamLayout;
    private readonly XFileReadStreamBlocks? _streamBlocks;
    private readonly IList<string>? _warnings;
    private readonly List<IQueuedXFilePointerResolver> _inlineResolvers = new();
    private readonly List<IQueuedXFilePointerResolver> _deferredResolvers = new();
    private readonly List<MaterializedPointerSpan> _materializedSpans = new();
    private readonly Dictionary<int, MaterializedSpanBlockIndex> _materializedSpanIndexes = new();
    private readonly Dictionary<OffsetPointerAddress, List<OffsetPointerTarget>> _pendingOffsetPointers = new();
    private readonly Dictionary<int, SortedSet<int>> _pendingOffsetKeysByBlock = new();
    private readonly Dictionary<OffsetStringLookupKey, string> _offsetStringCache = new();
    private readonly List<StringTableCell[]> _stringTableCellSets = new();
    private int _inlineResolverHead;
    private int _unresolvedOffsetPointerCount;
    private bool _deferInlinePointers;

    public XFileReadContext(
        ReadOnlySpan<byte> span,
        int position,
        XFileStreamLayout? streamLayout = null,
        IList<string>? warnings = null)
    {
        _span = span;
        _streamLayout = streamLayout;
        _streamBlocks = null;
        _warnings = warnings;
        Position = position;
    }

    public XFileReadContext(
        ReadOnlySpan<byte> span,
        int position,
        XFileReadStreamBlocks? streamBlocks,
        IList<string>? warnings = null)
    {
        _span = span;
        _streamLayout = null;
        _streamBlocks = streamBlocks;
        _warnings = warnings;
        Position = position;
    }

    public int Position;

    public ReadOnlySpan<byte> Span => _span;

    internal int CurrentAssetIndex { get; private set; } = -1;

    internal XAssetType CurrentAssetType { get; private set; }

    internal void SetCurrentAsset(int index, XAssetType type)
    {
        CurrentAssetIndex = index;
        CurrentAssetType = type;
    }

    public bool PushInlinePointerDeferral(bool deferInlinePointers = true)
    {
        var previous = _deferInlinePointers;
        _deferInlinePointers = deferInlinePointers;
        return previous;
    }

    public void RestoreInlinePointerDeferral(bool deferInlinePointers)
    {
        _deferInlinePointers = deferInlinePointers;
    }

    public int ReadInt32()
    {
        EnsureAvailable(4, "Int32");
        var value = _span.ReadInt32(ref Position);
        AdvanceStream(4);
        return value;
    }

    public ushort ReadUInt16()
    {
        EnsureAvailable(2, "UInt16");
        var value = _span.ReadUInt16(ref Position);
        AdvanceStream(2);
        return value;
    }

    public uint ReadUInt32()
    {
        EnsureAvailable(4, "UInt32");
        var value = _span.ReadUInt32(ref Position);
        AdvanceStream(4);
        return value;
    }

    public ulong ReadUInt64()
    {
        EnsureAvailable(8, "UInt64");
        var value = _span.ReadUInt64(ref Position);
        AdvanceStream(8);
        return value;
    }

    public float ReadFloat()
    {
        EnsureAvailable(4, "Float");
        var value = _span.ReadFloat(ref Position);
        AdvanceStream(4);
        return value;
    }

    public byte ReadByte()
    {
        EnsureAvailable(1, "Byte");
        var value = _span.ReadByte(ref Position);
        AdvanceStream(1);
        return value;
    }

    public bool ReadBool()
    {
        var start = Position;

        try
        {
            EnsureAvailable(1, "Boolean");
            var value = _span.ReadBool(ref Position);
            AdvanceStream(1);
            return value;
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw ReadFailure("Boolean", start, ex);
        }
    }

    public Vec4 ReadVec4()
    {
        EnsureAvailable(16, "Vec4");
        var value = _span.ReadVec4(ref Position);
        AdvanceStream(16);
        return value;
    }

    public string ReadCString()
    {
        var start = Position;

        try
        {
            var value = _span.ReadCStringAt(ref Position);
            AdvanceStream(Position - start);
            return value;
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw ReadFailure("CString", start, ex);
        }
    }

    public string ReadAlignedCString(int alignment = 4)
    {
        var value = ReadCString();
        AlignPosition(alignment);
        return value;
    }

    public string ReadString(int length)
    {
        EnsureAvailable(length, $"String[{length}]");
        var value = _span.ReadString(ref Position, length);
        AdvanceStream(length);
        return value;
    }

    public byte[] ReadBytes(int length)
    {
        EnsureAvailable(length, $"Byte[{length}]");
        var value = _span.Read(ref Position, length);
        AdvanceStream(length);
        return value;
    }

    public ZonePointer<T> ReadPointer<T>()
    {
        return ReadPointer<T>(PointerResolutionKind.Unknown);
    }

    public ZonePointer<T> ReadPointer<T>(
        PointerResolutionKind resolutionKind,
        string? fieldPath = null)
    {
        return ReadPointerCore<T, ZonePointer<T>>(
            raw => new ZonePointer<T>(raw),
            resolutionKind,
            fieldPath);
    }

    private TPointer ReadPointerCore<T, TPointer>(
        Func<int, TPointer> createPointer,
        PointerResolutionKind resolutionKind,
        string? fieldPath = null)
        where TPointer : ZonePointer<T>
    {
        EnsureAvailable(4, $"Pointer<{typeof(T).Name}>");
        var pointerFieldOffset = Position;
        var pointerFieldStreamAddress = GetActiveStreamAddress();
        var raw = _span.ReadInt32(ref Position);
        var pointer = createPointer(raw);
        AdvanceStream(4);
        RegisterPointer(pointer, typeof(T), pointerFieldOffset, pointerFieldStreamAddress, resolutionKind, fieldPath);

        return pointer;
    }

    public DirectPointer<T> ReadDirectPointer<T>(string? fieldPath = null)
    {
        return ReadPointerCore<T, DirectPointer<T>>(
            raw => new DirectPointer<T>(raw),
            PointerResolutionKind.Direct,
            fieldPath);
    }

    public AliasPointer<T> ReadAliasPointer<T>(string? fieldPath = null)
    {
        return ReadPointerCore<T, AliasPointer<T>>(
            raw => new AliasPointer<T>(raw),
            PointerResolutionKind.Alias,
            fieldPath);
    }

    public ZonePointer<T> CreatePointer<T>(
        int raw,
        bool register = true,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        return CreatePointerCore<T, ZonePointer<T>>(
            raw,
            value => new ZonePointer<T>(value),
            register,
            resolutionKind,
            fieldPath);
    }

    public DirectPointer<T> CreateDirectPointer<T>(
        int raw,
        bool register = true,
        string? fieldPath = null)
    {
        return CreatePointerCore<T, DirectPointer<T>>(
            raw,
            value => new DirectPointer<T>(value),
            register,
            PointerResolutionKind.Direct,
            fieldPath);
    }

    public AliasPointer<T> CreateAliasPointer<T>(
        int raw,
        bool register = true,
        string? fieldPath = null)
    {
        return CreatePointerCore<T, AliasPointer<T>>(
            raw,
            value => new AliasPointer<T>(value),
            register,
            PointerResolutionKind.Alias,
            fieldPath);
    }

    private TPointer CreatePointerCore<T, TPointer>(
        int raw,
        Func<int, TPointer> createPointer,
        bool register,
        PointerResolutionKind resolutionKind,
        string? fieldPath = null)
        where TPointer : ZonePointer<T>
    {
        var pointer = createPointer(raw);
        if (register)
            RegisterPointer(
                pointer,
                typeof(T),
                Position - 4,
                GetPreviousPointerFieldStreamAddress(),
                resolutionKind,
                fieldPath);

        return pointer;
    }

    public void RegisterPointer<T>(
        ZonePointer<T> pointer,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        RegisterPointer(
            pointer,
            typeof(T),
            Position - 4,
            GetPreviousPointerFieldStreamAddress(),
            resolutionKind,
            fieldPath);
    }

    public ZonePointer<T> ReadPointer<T>(
        XFileValueReader<T> reader,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        return ReadPointer<T>((ref XFileReadContext context, ZonePointer<T> pointer) =>
        {
            var value = context.ReadPointerValue(pointer, reader);
            pointer.SetResult(value);
        }, resolutionKind, fieldPath);
    }

    public ZonePointer<T> ReadPointer<T>(
        XFilePointerResolver<T> resolver,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        var pointer = ReadPointer<T>(resolutionKind, fieldPath);
        ResolvePointer(pointer, resolver);

        return pointer;
    }

    public ZonePointer<T> ReadInlinePointer<T>(
        XFilePointerResolver<T> resolver,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        var pointer = ReadPointer<T>(resolutionKind, fieldPath);
        ResolveInlinePointer(pointer, resolver);

        return pointer;
    }

    public void ResolvePointer<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    AddInlineResolver(pointer, resolver);
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        AddInlineResolver(pointer, resolver);
                    else
                        pointer.SetResult(default);
                    break;
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    public void ResolvePointerAligned<T>(
        ZonePointer<T> pointer,
        XFileStreamAlignment alignment,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    AddInlineResolver(pointer, resolver, streamAlignment: alignment);
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        AddInlineResolver(pointer, resolver, streamAlignment: alignment);
                    else
                        pointer.SetResult(default);
                    break;
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    public void ResolveInlinePointer<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    AddInlineResolver(pointer, resolver);
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        AddInlineResolver(pointer, resolver);
                    else
                        pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    public void ResolveInlinePointerDeferred<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver)
    {
        ResolveInlinePointerDeferred(pointer, resolver, materializationBlock: null);
    }

    public void ResolveInlinePointerDeferredInBlock<T>(
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFilePointerResolver<T> resolver)
    {
        ResolveInlinePointerDeferred(pointer, resolver, block);
    }

    private void ResolveInlinePointerDeferred<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver,
        XFILE_BLOCK? materializationBlock)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver, materializationBlock));
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver, materializationBlock));
                    else
                        pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    public void ResolvePointerDeferred<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver));
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver));
                    else
                        pointer.SetResult(default);
                    break;
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    public void ResolvePointerInBlock<T>(
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    AddInlineResolver(pointer, resolver, block);
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        AddInlineResolver(pointer, resolver, block);
                    else
                        pointer.SetResult(default);
                    break;
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    internal void ResolvePointerNowInBlock<T>(
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFilePointerResolver<T> resolver)
    {
        PushStreamBlock(block);
        try
        {
            ResolveInlinePointerNow(pointer, resolver);
        }
        finally
        {
            PopStreamBlock();
        }
    }

    internal void ResolvePointerAlignedNowInBlock<T>(
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFileStreamAlignment alignment,
        XFilePointerResolver<T> resolver)
    {
        PushStreamBlock(block);
        try
        {
            ResolveInlinePointerAlignedNow(pointer, alignment, resolver);
        }
        finally
        {
            PopStreamBlock();
        }
    }

    public void ResolvePointerAlignedInBlock<T>(
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFileStreamAlignment alignment,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        try
        {
            switch (pointer.Kind)
            {
                case PointerKind.Null:
                    pointer.SetResult(default);
                    break;
                case PointerKind.Inline:
                    AddInlineResolver(pointer, resolver, block, alignment);
                    break;
                case PointerKind.Insert:
                    if (CanMaterializeInlinePointer(pointer))
                        AddInlineResolver(pointer, resolver, block, alignment);
                    else
                        pointer.SetResult(default);
                    break;
                case PointerKind.Offset:
                    pointer.SetResult(default);
                    break;
            }
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    internal void ResolveInlinePointerNow<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        switch (pointer.Kind)
        {
            case PointerKind.Null:
                pointer.SetResult(default);
                return;
            case PointerKind.Offset:
                pointer.SetResult(default);
                return;
            case PointerKind.Inline:
                break;
            case PointerKind.Insert:
                if (CanMaterializeInlinePointer(pointer))
                    break;

                pointer.SetResult(default);
                return;
        }

        var start = Position;
        PrepareInlinePointerMaterialization(pointer);
        var streamStart = GetActiveStreamAddress();
        Memory.ResolvePointer(pointer, Position);
        ResolvePointerStreamAddress(pointer, streamStart);

        try
        {
            resolver(ref this, pointer);
            var length = Position - start;
            pointer.SetSourceSpan(start, length);
            RegisterMaterializedPointer(pointer, typeof(T), start, length, streamStart);
        }
        catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
        {
            throw PointerFailure(pointer, typeof(T).Name, ex);
        }
    }

    internal void ResolveInlinePointerAlignedNow<T>(
        ZonePointer<T> pointer,
        XFileStreamAlignment alignment,
        XFilePointerResolver<T> resolver)
    {
        if (pointer.IsResolved)
            return;

        switch (pointer.Kind)
        {
            case PointerKind.Null:
                pointer.SetResult(default);
                return;
            case PointerKind.Offset:
                pointer.SetResult(default);
                return;
            case PointerKind.Inline:
                break;
            case PointerKind.Insert:
                if (CanMaterializeInlinePointer(pointer))
                    break;

                pointer.SetResult(default);
                return;
        }

        AlignStreamAndPosition(alignment);
        ResolveInlinePointerNow(pointer, resolver);
    }

    public T ReadPointerValue<T>(
        ZonePointer<T> pointer,
        XFileValueReader<T> reader)
    {
        var start = Position;
        PrepareInlinePointerMaterialization(pointer);
        var streamStart = GetActiveStreamAddress();
        Memory.ResolvePointer(pointer, Position);
        ResolvePointerStreamAddress(pointer, streamStart);

        if (pointer.Kind != PointerKind.Null
            && pointer.Kind != PointerKind.Offset
            && CanMaterializeInlinePointer(pointer))
        {
            try
            {
                var value = reader(ref this);
                var length = Position - start;
                pointer.SetSourceSpan(start, length);
                RegisterMaterializedPointer(pointer, typeof(T), start, length, streamStart);
                return value;
            }
            catch (Exception ex) when (ex is not InvalidDataException { InnerException: not null })
            {
                throw PointerFailure(pointer, typeof(T).Name, ex);
            }
        }

        return default!;
    }

    public void ResolveQueued()
    {
        var resolvedCount = 0;

        while (HasPendingInlineResolvers || _deferredResolvers.Count > 0)
        {
            if (++resolvedCount > 1_000_000)
            {
                throw new InvalidDataException(
                    $"Stopped resolving inline zone pointers after {resolvedCount:N0} entries at zone offset 0x{Position:X8} ({Position:N0}); remaining queued pointers: {PendingInlineResolverCount:N0}, deferred pointers: {_deferredResolvers.Count:N0}.");
            }

            if (!HasPendingInlineResolvers)
            {
                _inlineResolvers.Clear();
                _inlineResolverHead = 0;
                _inlineResolvers.AddRange(_deferredResolvers);
                _deferredResolvers.Clear();
            }

            var resolver = _inlineResolvers[_inlineResolverHead++];

            var start = Position;
            var streamStart = GetActiveStreamAddress();
            var olderSiblingCount = _inlineResolvers.Count - _inlineResolverHead;
            var olderInlineTail = _inlineResolvers.Count;
            var olderDeferredCount = _deferredResolvers.Count;
            XFileReadTrace.TraceResolver(
                "begin",
                resolver,
                start,
                streamStart,
                CurrentAssetIndex,
                CurrentAssetType,
                _inlineResolverHead - 1,
                olderSiblingCount,
                _deferredResolvers.Count,
                0);
            resolver.Resolve(ref this);

            var nestedCount = _inlineResolvers.Count - olderInlineTail;
            var nestedDeferredCount = _deferredResolvers.Count - olderDeferredCount;
            XFileReadTrace.TraceResolver(
                "end",
                resolver,
                start,
                streamStart,
                CurrentAssetIndex,
                CurrentAssetType,
                _inlineResolverHead - 1,
                olderSiblingCount,
                _deferredResolvers.Count,
                Position - start);
            var deferredInsertIndex = _inlineResolverHead;
            if (nestedCount > 0 && olderSiblingCount > 0)
            {
                var nestedResolvers = _inlineResolvers.GetRange(olderInlineTail, nestedCount);
                _inlineResolvers.RemoveRange(olderInlineTail, nestedCount);

                _inlineResolvers.InsertRange(_inlineResolverHead, nestedResolvers);
            }

            if (nestedCount > 0)
                deferredInsertIndex += nestedCount;

            if (nestedDeferredCount > 0)
            {
                var nestedDeferredResolvers = _deferredResolvers.GetRange(olderDeferredCount, nestedDeferredCount);
                _deferredResolvers.RemoveRange(olderDeferredCount, nestedDeferredCount);

                _inlineResolvers.InsertRange(deferredInsertIndex, nestedDeferredResolvers);
            }
        }

        _inlineResolvers.Clear();
        _inlineResolverHead = 0;
    }

    internal XFileResolverScope CaptureResolverScope()
    {
        return new XFileResolverScope(_inlineResolvers.Count, _deferredResolvers.Count);
    }

    internal void ResolveResolverScopeNow(XFileResolverScope scope)
    {
        var scopedResolvers = ExtractResolverRange(_inlineResolvers, scope.InlineCount);
        scopedResolvers.AddRange(ExtractResolverRange(_deferredResolvers, scope.DeferredCount));

        var resolverIndex = 0;
        while (resolverIndex < scopedResolvers.Count)
        {
            if (++resolverIndex > 1_000_000)
            {
                throw new InvalidDataException(
                    $"Stopped resolving scoped inline zone pointers at zone offset 0x{Position:X8} ({Position:N0}); remaining scoped pointers: {scopedResolvers.Count - resolverIndex:N0}.");
            }

            var resolver = scopedResolvers[resolverIndex - 1];
            var start = Position;
            var streamStart = GetActiveStreamAddress();
            var olderSiblingCount = scopedResolvers.Count - resolverIndex;
            var olderInlineTail = _inlineResolvers.Count;
            var olderDeferredCount = _deferredResolvers.Count;

            XFileReadTrace.TraceResolver(
                "begin",
                resolver,
                start,
                streamStart,
                CurrentAssetIndex,
                CurrentAssetType,
                resolverIndex - 1,
                olderSiblingCount,
                _deferredResolvers.Count,
                0);

            resolver.Resolve(ref this);

            var nestedResolvers = ExtractResolverRange(_inlineResolvers, olderInlineTail);
            var nestedDeferredResolvers = ExtractResolverRange(_deferredResolvers, olderDeferredCount);
            if (nestedDeferredResolvers.Count > 0)
                nestedResolvers.AddRange(nestedDeferredResolvers);

            XFileReadTrace.TraceResolver(
                "end",
                resolver,
                start,
                streamStart,
                CurrentAssetIndex,
                CurrentAssetType,
                resolverIndex - 1,
                olderSiblingCount,
                _deferredResolvers.Count,
                Position - start);

            if (nestedResolvers.Count > 0)
                scopedResolvers.InsertRange(resolverIndex, nestedResolvers);
        }
    }

    public void FinalizeOffsetPointerBindings()
    {
        BindRemainingOffsetPointersToContainingSpans();

        if (_unresolvedOffsetPointerCount > 0 && _streamBlocks is null)
            _warnings?.Add(
                $"Unable to match {_unresolvedOffsetPointerCount:N0} offset pointer target(s) to materialized zone data spans.");

        ApplyStringTableLogicalValues();
    }

    private static List<IQueuedXFilePointerResolver> ExtractResolverRange(
        List<IQueuedXFilePointerResolver> resolvers,
        int start)
    {
        if (start < 0 || start >= resolvers.Count)
            return [];

        var count = resolvers.Count - start;
        var values = resolvers.GetRange(start, count);
        resolvers.RemoveRange(start, count);
        return values;
    }

    internal void RegisterStringTableCells(StringTableCell[] cells)
    {
        if (cells.Length > 0)
            _stringTableCellSets.Add(cells);
    }

    public void PromoteDeferredPointers()
    {
        if (_deferredResolvers.Count == 0)
            return;

        if (!HasPendingInlineResolvers)
        {
            _inlineResolvers.Clear();
            _inlineResolverHead = 0;
        }

        _inlineResolvers.InsertRange(_inlineResolverHead, _deferredResolvers);
        _deferredResolvers.Clear();
    }

    private bool HasPendingInlineResolvers => _inlineResolverHead < _inlineResolvers.Count;

    private int PendingInlineResolverCount => Math.Max(0, _inlineResolvers.Count - _inlineResolverHead);

    private static bool CanMaterializeInlinePointer<T>(ZonePointer<T> pointer)
    {
        return pointer.Kind switch
        {
            PointerKind.Inline => true,
            PointerKind.Insert => typeof(T) != typeof(string),
            _ => false,
        };
    }

    private void AddInlineResolver<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver,
        XFILE_BLOCK? materializationBlock = null,
        XFileStreamAlignment? streamAlignment = null)
    {
        var queuedResolver = new QueuedXFilePointerResolver<T>(
            pointer,
            resolver,
            materializationBlock,
            streamAlignment);
        if (_deferInlinePointers)
            _deferredResolvers.Add(queuedResolver);
        else
            _inlineResolvers.Add(queuedResolver);
    }

    internal void RegisterMaterializedPointer(
        Pointer pointer,
        Type valueType,
        int start,
        int length,
        XFileBlockAddress streamStart)
    {
        if (start < 0 || length <= 0)
            return;

        _materializedSpans.Add(new MaterializedPointerSpan(
            start,
            length,
            streamStart.BlockIndex,
            streamStart.Offset,
            valueType,
            pointer));

        var span = _materializedSpans[^1];
        AddMaterializedSpanToIndex(span.StreamBlockIndex, span);
        AddMaterializedSpanToIndex(-1, span);
        BindPendingOffsetPointers(span);
    }

    internal void PrepareInlinePointerMaterialization(Pointer pointer)
    {
        if (pointer.Kind != PointerKind.Insert
            || pointer.HasAliasCellStreamAddress
            || _streamBlocks is null)
        {
            return;
        }

        var slot = _streamBlocks.ReserveInsertSlot();
        pointer.SetAliasCellStreamAddress(slot.BlockIndex, slot.Offset);
    }

    private void RegisterPointer(
        Pointer pointer,
        Type valueType,
        int pointerFieldOffset,
        XFileBlockAddress pointerFieldStreamAddress,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        var proofedKind = EbootPointerRules.Resolve(
            pointer,
            resolutionKind,
            fieldPath,
            out var normalizedFieldPath);
        pointer.SetResolutionKind(proofedKind, string.IsNullOrEmpty(normalizedFieldPath) ? fieldPath : normalizedFieldPath);
        pointer.SetPointerFieldSourceSpan(
            pointerFieldOffset,
            XFileWriteRules.PointerSize,
            pointerFieldStreamAddress.BlockIndex,
            pointerFieldStreamAddress.Offset);
        XFileReadValidator.ValidatePointerShape(this, pointer, valueType);

        if (pointer.Kind != PointerKind.Offset)
            return;

        if (pointer.ResolutionKind == PointerResolutionKind.Alias)
        {
            pointer.SetAliasCellStreamAddress(pointer.StreamBlockIndex, pointer.Offset);
            return;
        }

        if (_streamBlocks is not null)
        {
            pointer.SetTargetOffset(pointer.Offset);
            RegisterOffsetPointerTarget(new OffsetPointerTarget(
                pointer,
                valueType,
                pointer.StreamBlockIndex,
                pointer.Offset,
                pointerFieldOffset));
            return;
        }

        if (_streamLayout is not null
            && _streamLayout.TryGetZoneOffset(pointer, out var targetOffset))
        {
            pointer.SetTargetOffset(targetOffset);
            RegisterOffsetPointerTarget(new OffsetPointerTarget(
                pointer,
                valueType,
                StreamBlockIndex: -1,
                targetOffset,
                pointerFieldOffset));
        }
    }

    private void AddMaterializedSpanToIndex(int streamBlockIndex, MaterializedPointerSpan span)
    {
        if (!_materializedSpanIndexes.TryGetValue(streamBlockIndex, out var index))
        {
            index = new MaterializedSpanBlockIndex();
            _materializedSpanIndexes.Add(streamBlockIndex, index);
        }

        index.Add(span);
    }

    private void RegisterOffsetPointerTarget(OffsetPointerTarget target)
    {
        if (TryBindOffsetPointer(target, allowContainingSpan: false))
            return;

        var address = new OffsetPointerAddress(target.StreamBlockIndex, target.TargetOffset);
        if (!_pendingOffsetPointers.TryGetValue(address, out var pointers))
        {
            pointers = new List<OffsetPointerTarget>();
            _pendingOffsetPointers.Add(address, pointers);
        }

        pointers.Add(target);

        if (!_pendingOffsetKeysByBlock.TryGetValue(target.StreamBlockIndex, out var keys))
        {
            keys = new SortedSet<int>();
            _pendingOffsetKeysByBlock.Add(target.StreamBlockIndex, keys);
        }

        keys.Add(target.TargetOffset);
        _unresolvedOffsetPointerCount++;
    }

    private bool TryBindOffsetPointer(OffsetPointerTarget target, bool allowContainingSpan)
    {
        var indexKey = target.StreamBlockIndex >= 0 ? target.StreamBlockIndex : -1;
        if (!_materializedSpanIndexes.TryGetValue(indexKey, out var index))
            return false;

        var span = index.FindBest(target.TargetOffset, target.ValueType, allowContainingSpan);
        if (span is null)
            return false;

        BindOffsetPointer(target, span.Value);
        return true;
    }

    private void BindPendingOffsetPointers(MaterializedPointerSpan span)
    {
        if (!_pendingOffsetKeysByBlock.TryGetValue(span.StreamBlockIndex, out var pendingOffsets)
            || pendingOffsets.Count == 0)
        {
            return;
        }

        var start = span.StreamOffset;
        var end = span.StreamOffset + span.Length - 1;
        var matchingOffsets = pendingOffsets.GetViewBetween(start, end).ToArray();
        foreach (var targetOffset in matchingOffsets)
        {
            var address = new OffsetPointerAddress(span.StreamBlockIndex, targetOffset);
            if (!_pendingOffsetPointers.TryGetValue(address, out var pointers))
                continue;

            for (var i = pointers.Count - 1; i >= 0; i--)
            {
                var target = pointers[i];
                if (!IsCompatiblePointerTarget(target.ValueType, span.ValueType))
                    continue;

                BindOffsetPointer(target, span);
                pointers.RemoveAt(i);
                _unresolvedOffsetPointerCount--;
            }

            if (pointers.Count != 0)
                continue;

            _pendingOffsetPointers.Remove(address);
            pendingOffsets.Remove(targetOffset);
        }
    }

    private void BindRemainingOffsetPointersToContainingSpans()
    {
        if (_pendingOffsetPointers.Count == 0)
            return;

        foreach (var item in _pendingOffsetPointers.ToArray())
        {
            var pointers = item.Value;
            for (var i = pointers.Count - 1; i >= 0; i--)
            {
                var target = pointers[i];
                if (!TryBindOffsetPointer(target, allowContainingSpan: true))
                    continue;

                pointers.RemoveAt(i);
                _unresolvedOffsetPointerCount--;
            }

            if (pointers.Count != 0)
                continue;

            _pendingOffsetPointers.Remove(item.Key);
            if (!_pendingOffsetKeysByBlock.TryGetValue(item.Key.StreamBlockIndex, out var offsets))
                continue;

            offsets.Remove(item.Key.TargetOffset);
            if (offsets.Count == 0)
                _pendingOffsetKeysByBlock.Remove(item.Key.StreamBlockIndex);
        }
    }

    private void BindOffsetPointer(OffsetPointerTarget target, MaterializedPointerSpan span)
    {
        target.Pointer.SetTargetSpan(
            span.Start,
            span.Length,
            span.StreamBlockIndex,
            span.StreamOffset);
        ResolveOffsetStringPointer(
            target.Pointer,
            span,
            target.TargetOffset);
    }

    private static bool IsBetterSpan(MaterializedPointerSpan span, MaterializedPointerSpan? currentBest)
    {
        return currentBest is null
            || span.Length < currentBest.Value.Length
            || (span.Length == currentBest.Value.Length
                && (span.StreamOffset < currentBest.Value.StreamOffset
                    || (span.StreamOffset == currentBest.Value.StreamOffset
                        && span.Start < currentBest.Value.Start)));
    }

    private static bool IsCompatiblePointerTarget(Type pointerType, Type spanType)
    {
        return pointerType == spanType
            || pointerType.IsAssignableFrom(spanType)
            || spanType.IsAssignableFrom(pointerType);
    }

    private static bool CanUseContainingPointerSpan(Type spanType)
    {
        return true;
    }

    private void ResolveOffsetStringPointer(
        Pointer pointer,
        MaterializedPointerSpan span,
        int targetOffset)
    {
        if (pointer is not ZonePointer<string> stringPointer || stringPointer.Result is not null)
            return;

        if (span.ValueType != typeof(string))
            return;

        var delta = targetOffset - span.StreamOffset;
        if (delta < 0 || delta >= span.Length)
            return;

        var key = new OffsetStringLookupKey(span.Start, span.Length, delta);
        if (!_offsetStringCache.TryGetValue(key, out var value))
        {
            value = ReadCStringAt(span.Start + delta, span.Start, span.Length);
            _offsetStringCache.Add(key, value);
        }

        stringPointer.SetResult(value);
    }

    private void ApplyStringTableLogicalValues()
    {
        foreach (var cells in _stringTableCellSets)
            StringTableCell.ApplyLogicalStringValues(cells);
    }

    private string ReadCStringAt(int offset, int spanStart, int spanLength)
    {
        var spanEnd = Math.Min(_span.Length, spanStart + spanLength);
        if (offset < spanStart || offset >= spanEnd)
            return string.Empty;

        var end = offset;
        while (end < spanEnd && _span[end] != 0)
            end++;

        if (end <= offset)
            return string.Empty;

        var readOffset = offset;
        return _span.ReadString(ref readOffset, end - offset);
    }

    public void AlignPosition(int alignment)
    {
        if (alignment <= 0)
            throw new InvalidDataException($"Cannot align zone position with invalid alignment {alignment:N0}.");

        var remainder = Position % alignment;
        if (remainder == 0)
            return;

        var padding = alignment - remainder;
        Position += padding;
        AdvanceStream(padding);
    }

    public void AlignStreamOnly(XFileStreamAlignment alignment)
    {
        _streamBlocks?.Align((int)alignment);
    }

    public void AlignStreamAndPosition(XFileStreamAlignment alignment)
    {
        if (_streamBlocks is null)
        {
            AlignPosition((int)alignment);
            return;
        }

        var padding = _streamBlocks.AlignAndGetPadding((int)alignment);
        if (padding == 0)
            return;

        EnsureAvailable(padding, $"Stream alignment padding ({alignment})");
        Position += padding;
    }

    public void PushStreamBlock(XFILE_BLOCK block)
    {
        _streamBlocks?.PushStreamBlock(block);
    }

    public void PopStreamBlock()
    {
        _streamBlocks?.PopStreamBlock();
    }

    public XFileBlockAddress AlignStream(int alignment)
    {
        return _streamBlocks?.Align(alignment)
            ?? new XFileBlockAddress(0, Position);
    }

    internal XFileBlockAddress GetActiveStreamAddress()
    {
        return _streamBlocks?.ActiveAddress
            ?? new XFileBlockAddress(0, Position);
    }

    internal bool TryGetStreamBlockSize(int streamBlockIndex, out int blockSize)
    {
        if (_streamBlocks is not null)
            return _streamBlocks.TryGetBlockSize(streamBlockIndex, out blockSize);

        if (_streamLayout is not null)
            return _streamLayout.TryGetBlockSize(streamBlockIndex, out blockSize);

        blockSize = 0;
        return false;
    }

    private XFileBlockAddress GetPreviousPointerFieldStreamAddress()
    {
        var active = GetActiveStreamAddress();
        return new XFileBlockAddress(active.BlockIndex, Math.Max(0, active.Offset - 4));
    }

    internal void ResolvePointerStreamAddress(Pointer pointer, XFileBlockAddress streamAddress)
    {
        if (!pointer.IsInlineData)
            return;

        pointer.SetStreamAddress(streamAddress.BlockIndex, streamAddress.Offset);
    }

    private void AdvanceStream(int byteCount)
    {
        _streamBlocks?.Advance(byteCount);
    }

    private void EnsureAvailable(int length, string operation)
    {
        if (length < 0)
            throw new InvalidDataException($"Cannot read {operation} with negative length {length:N0} at zone offset 0x{Position:X8} ({Position:N0}).");

        if (Position < 0 || Position + length > _span.Length)
            throw new InvalidDataException($"Cannot read {operation} ({length:N0} byte(s)) at zone offset 0x{Position:X8} ({Position:N0}); zone length is 0x{_span.Length:X8} ({_span.Length:N0}).");
    }

    private static InvalidDataException ReadFailure(string operation, int position, Exception innerException)
    {
        return new InvalidDataException($"Failed to read {operation} at zone offset 0x{position:X8} ({position:N0}).", innerException);
    }

    private InvalidDataException PointerFailure<T>(ZonePointer<T> pointer, string typeName, Exception innerException)
    {
        return new InvalidDataException(
            $"Failed to resolve {pointer.Kind} pointer for {typeName}; raw=0x{pointer.Raw:X8}, offset=0x{pointer.Offset:X8}, current zone offset=0x{Position:X8} ({Position:N0}).",
            innerException);
    }

    private readonly record struct MaterializedPointerSpan(
        int Start,
        int Length,
        int StreamBlockIndex,
        int StreamOffset,
        Type ValueType,
        Pointer Pointer)
    {
        public bool Contains(int value) => value >= Start && value < Start + Length;
        public bool ContainsStreamOffset(int value) => value >= StreamOffset && value < StreamOffset + Length;
    }

    private sealed class MaterializedSpanBlockIndex
    {
        private readonly List<MaterializedPointerSpan> _spans = new();
        private int[] _prefixMaxEnds = [];
        private bool _isSorted = true;

        public void Add(MaterializedPointerSpan span)
        {
            if (_spans.Count > 0 && CompareSpans(_spans[^1], span) > 0)
                _isSorted = false;

            _spans.Add(span);
            _prefixMaxEnds = [];
        }

        public MaterializedPointerSpan? FindBest(int targetOffset, Type valueType, bool allowContainingSpan)
        {
            EnsureIndexed();

            var index = FindLastSpanStartingBeforeOrAt(targetOffset);
            if (index < 0)
                return null;

            MaterializedPointerSpan? bestCompatibleSpan = null;
            MaterializedPointerSpan? bestContainingSpan = null;
            for (var i = index; i >= 0; i--)
            {
                if (_prefixMaxEnds[i] <= targetOffset)
                    break;

                var span = _spans[i];
                if (!span.ContainsStreamOffset(targetOffset))
                    continue;

                if (IsCompatiblePointerTarget(valueType, span.ValueType))
                {
                    if (IsBetterSpan(span, bestCompatibleSpan))
                        bestCompatibleSpan = span;
                    continue;
                }

                if (!allowContainingSpan || !CanUseContainingPointerSpan(span.ValueType))
                    continue;

                if (IsBetterSpan(span, bestContainingSpan))
                    bestContainingSpan = span;
            }

            return bestCompatibleSpan ?? bestContainingSpan;
        }

        private void EnsureIndexed()
        {
            if (_prefixMaxEnds.Length == _spans.Count)
                return;

            if (!_isSorted)
            {
                _spans.Sort(CompareSpans);
                _isSorted = true;
            }

            _prefixMaxEnds = new int[_spans.Count];
            var maxEnd = 0;
            for (var i = 0; i < _spans.Count; i++)
            {
                maxEnd = Math.Max(maxEnd, _spans[i].StreamOffset + _spans[i].Length);
                _prefixMaxEnds[i] = maxEnd;
            }
        }

        private int FindLastSpanStartingBeforeOrAt(int targetOffset)
        {
            var result = -1;
            var low = 0;
            var high = _spans.Count - 1;

            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                if (_spans[mid].StreamOffset <= targetOffset)
                {
                    result = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return result;
        }

        private static int CompareSpans(MaterializedPointerSpan left, MaterializedPointerSpan right)
        {
            var startComparison = left.StreamOffset.CompareTo(right.StreamOffset);
            if (startComparison != 0)
                return startComparison;

            var lengthComparison = left.Length.CompareTo(right.Length);
            return lengthComparison != 0
                ? lengthComparison
                : left.Start.CompareTo(right.Start);
        }
    }

    private readonly record struct OffsetPointerAddress(
        int StreamBlockIndex,
        int TargetOffset);

    private readonly record struct OffsetStringLookupKey(
        int SpanStart,
        int SpanLength,
        int Delta);

    private readonly record struct OffsetPointerTarget(
        Pointer Pointer,
        Type ValueType,
        int StreamBlockIndex,
        int TargetOffset,
        int PointerFieldOffset);
}
