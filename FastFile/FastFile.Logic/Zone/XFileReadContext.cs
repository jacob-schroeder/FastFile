using FastFile.Models.Data;
using FastFile.Logic.Extensions;
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
    void Resolve(ref XFileReadContext context);
}

internal sealed class QueuedXFilePointerResolver<T>(
    ZonePointer<T> pointer,
    XFilePointerResolver<T> resolver,
    XFILE_BLOCK? materializationBlock = null) : IQueuedXFilePointerResolver
{
    public string Name => typeof(T).Name;

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

internal ref struct XFileReadContext
{
    private readonly ReadOnlySpan<byte> _span;
    private readonly XFileStreamLayout? _streamLayout;
    private readonly XFileReadStreamBlocks? _streamBlocks;
    private readonly IList<string>? _warnings;
    private readonly List<IQueuedXFilePointerResolver> _inlineResolvers = new();
    private readonly List<IQueuedXFilePointerResolver> _deferredResolvers = new();
    private readonly List<MaterializedPointerSpan> _materializedSpans = new();
    private readonly List<OffsetPointerTarget> _offsetPointers = new();
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
        EnsureAvailable(4, $"Pointer<{typeof(T).Name}>");
        var pointerFieldOffset = Position;
        var pointerFieldStreamAddress = GetActiveStreamAddress();
        var pointer = Memory.ReadPointer<T>(_span, ref Position);
        AdvanceStream(4);
        RegisterPointer(pointer, typeof(T), pointerFieldOffset, pointerFieldStreamAddress, resolutionKind, fieldPath);

        return pointer;
    }

    public ZonePointer<T> ReadDirectPointer<T>(string? fieldPath = null)
    {
        return ReadPointer<T>(PointerResolutionKind.Direct, fieldPath);
    }

    public ZonePointer<T> ReadAliasPointer<T>(string? fieldPath = null)
    {
        return ReadPointer<T>(PointerResolutionKind.Alias, fieldPath);
    }

    public ZonePointer<T> CreatePointer<T>(
        int raw,
        bool register = true,
        PointerResolutionKind resolutionKind = PointerResolutionKind.Unknown,
        string? fieldPath = null)
    {
        var pointer = new ZonePointer<T>(raw);
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
                case PointerKind.Insert:
                    AddInlineResolver(pointer, resolver);
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
                case PointerKind.Insert:
                    AddInlineResolver(pointer, resolver);
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
                case PointerKind.Insert:
                    _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver, materializationBlock));
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
                case PointerKind.Insert:
                    _deferredResolvers.Add(new QueuedXFilePointerResolver<T>(pointer, resolver));
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
                case PointerKind.Insert:
                    AddInlineResolver(pointer, resolver, block);
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
            case PointerKind.Insert:
                break;
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

    public T ReadPointerValue<T>(
        ZonePointer<T> pointer,
        XFileValueReader<T> reader)
    {
        var start = Position;
        PrepareInlinePointerMaterialization(pointer);
        var streamStart = GetActiveStreamAddress();
        Memory.ResolvePointer(pointer, Position);
        ResolvePointerStreamAddress(pointer, streamStart);

        if (pointer.Kind != PointerKind.Offset)
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

        while (_inlineResolvers.Count > 0 || _deferredResolvers.Count > 0)
        {
            if (++resolvedCount > 1_000_000)
            {
                throw new InvalidDataException(
                    $"Stopped resolving inline zone pointers after {resolvedCount:N0} entries at zone offset 0x{Position:X8} ({Position:N0}); remaining queued pointers: {_inlineResolvers.Count:N0}, deferred pointers: {_deferredResolvers.Count:N0}.");
            }

            if (_inlineResolvers.Count == 0)
            {
                _inlineResolvers.AddRange(_deferredResolvers);
                _deferredResolvers.Clear();
            }

            var resolver = _inlineResolvers[0];
            _inlineResolvers.RemoveAt(0);

            var olderSiblingCount = _inlineResolvers.Count;
            var olderDeferredCount = _deferredResolvers.Count;
            resolver.Resolve(ref this);

            var nestedCount = _inlineResolvers.Count - olderSiblingCount;
            var nestedDeferredCount = _deferredResolvers.Count - olderDeferredCount;
            var deferredInsertIndex = 0;
            if (nestedCount > 0 && olderSiblingCount > 0)
            {
                var nestedResolvers = _inlineResolvers.GetRange(olderSiblingCount, nestedCount);
                _inlineResolvers.RemoveRange(olderSiblingCount, nestedCount);

                _inlineResolvers.InsertRange(0, nestedResolvers);
                deferredInsertIndex = nestedResolvers.Count;
            }

            if (nestedDeferredCount > 0)
            {
                var nestedDeferredResolvers = _deferredResolvers.GetRange(olderDeferredCount, nestedDeferredCount);
                _deferredResolvers.RemoveRange(olderDeferredCount, nestedDeferredCount);

                _inlineResolvers.InsertRange(deferredInsertIndex, nestedDeferredResolvers);
            }
        }
    }

    public void ResolveOffsetPointers()
    {
        if (_offsetPointers.Count == 0 || _materializedSpans.Count == 0)
            return;

        _materializedSpans.Sort((left, right) =>
        {
            var blockComparison = left.StreamBlockIndex.CompareTo(right.StreamBlockIndex);
            if (blockComparison != 0)
                return blockComparison;

            var startComparison = left.StreamOffset.CompareTo(right.StreamOffset);
            return startComparison != 0
                ? startComparison
                : left.Length.CompareTo(right.Length);
        });

        var unresolvedCount = 0;
        foreach (var offsetPointer in _offsetPointers)
        {
            var bestSpan = FindBestMaterializedSpan(
                offsetPointer.StreamBlockIndex,
                offsetPointer.TargetOffset,
                offsetPointer.ValueType,
                offsetPointer.PointerFieldOffset);
            if (bestSpan is null)
            {
                unresolvedCount++;
                continue;
            }

            offsetPointer.Pointer.SetTargetSpan(
                bestSpan.Value.Start,
                bestSpan.Value.Length,
                bestSpan.Value.StreamBlockIndex,
                bestSpan.Value.StreamOffset);
        }

        if (unresolvedCount > 0 && _streamBlocks is null)
        {
            _warnings?.Add(
                $"Unable to match {unresolvedCount:N0} offset pointer target(s) to materialized zone data spans.");
        }
    }

    public void PromoteDeferredPointers()
    {
        if (_deferredResolvers.Count == 0)
            return;

        _inlineResolvers.InsertRange(0, _deferredResolvers);
        _deferredResolvers.Clear();
    }

    private void AddInlineResolver<T>(
        ZonePointer<T> pointer,
        XFilePointerResolver<T> resolver,
        XFILE_BLOCK? materializationBlock = null)
    {
        var queuedResolver = new QueuedXFilePointerResolver<T>(pointer, resolver, materializationBlock);
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
            _offsetPointers.Add(new OffsetPointerTarget(
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
            _offsetPointers.Add(new OffsetPointerTarget(
                pointer,
                valueType,
                StreamBlockIndex: -1,
                targetOffset,
                pointerFieldOffset));
        }
    }

    private MaterializedPointerSpan? FindBestMaterializedSpan(
        int streamBlockIndex,
        int targetOffset,
        Type valueType,
        int pointerFieldOffset)
    {
        MaterializedPointerSpan? bestCompatibleSpan = null;
        MaterializedPointerSpan? bestContainingSpan = null;
        foreach (var span in _materializedSpans)
        {
            if (streamBlockIndex >= 0 && span.StreamBlockIndex != streamBlockIndex)
                continue;

            if (!span.ContainsStreamOffset(targetOffset))
                continue;

            if (IsCompatiblePointerTarget(valueType, span.ValueType))
            {
                if (bestCompatibleSpan is null || span.Length < bestCompatibleSpan.Value.Length)
                    bestCompatibleSpan = span;
                continue;
            }

            if (!CanUseContainingPointerSpan(span.ValueType))
                continue;

            if (bestContainingSpan is null || span.Length < bestContainingSpan.Value.Length)
                bestContainingSpan = span;
        }

        return bestCompatibleSpan ?? bestContainingSpan;
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

    private readonly record struct OffsetPointerTarget(
        Pointer Pointer,
        Type ValueType,
        int StreamBlockIndex,
        int TargetOffset,
        int PointerFieldOffset);
}
