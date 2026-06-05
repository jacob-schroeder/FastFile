using FastFile.Models.Data;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriterContext
{
    public void RegisterMaterializedPointerValue(Pointer? pointer, int? writtenLength = null)
    {
        if (pointer is null)
            return;

        var address = ActiveAddress;
        if (pointer.Kind == PointerKind.Insert)
        {
            var slot = ReserveInsertSlot();
            address = ActiveAddress;

            if (pointer.HasAliasCellStreamAddress)
            {
                _writtenAliasCellsBySource[new XFileBlockAddress(
                    pointer.AliasCellStreamBlockIndex,
                    pointer.AliasCellStreamOffset)] = slot;
            }
        }

        if (!pointer.HasSourceSpan)
            return;

        RegisterWrittenSourceSpan(new WrittenPointerValueSpan(
            pointer.SourceOffset,
            pointer.SourceLength,
            writtenLength ?? pointer.SourceLength,
            pointer.StreamBlockIndex,
            pointer.Offset,
            address),
            indexEachByte: true);
    }

    private void RegisterWrittenSourceSpan(WrittenPointerValueSpan value, bool indexEachByte)
    {
        if (value.SourceLength <= 0)
            return;

        _writtenPointerValues.Add(value);

        if (!indexEachByte)
            return;

        var mappedLength = Math.Min(value.SourceLength, value.WrittenLength);
        for (var delta = 0; delta < mappedLength; delta++)
        {
            _writtenPointerValuesBySourceStreamAddress.TryAdd(
                new XFileBlockAddress(value.SourceStreamBlockIndex, value.SourceStreamOffset + delta),
                new XFileBlockAddress(value.Address.BlockIndex, value.Address.Offset + delta));
        }
    }

    public void ResolveOffsetPointerFields()
    {
        if (_pendingOffsetPointerFields.Count == 0)
            return;

        if (_writtenPointerValues.Count == 0)
            return;

        foreach (var pending in _pendingOffsetPointerFields)
        {
            if (!TryGetWrittenTarget(pending.Pointer, out var target))
                continue;

            WriteInt32AtSerializedOffset(pending.SerializedOffset, target.Raw);
        }
    }

    private bool TryGetWrittenTarget(Pointer pointer, out XFileBlockAddress target)
    {
        target = default;

        if (pointer.ResolutionKind == PointerResolutionKind.Unknown)
            return false;

        if (pointer.ResolutionKind == PointerResolutionKind.Alias)
            return TryGetWrittenAliasCell(pointer, out target)
                || TryGetWrittenTargetBySourceStreamAddress(pointer.StreamBlockIndex, pointer.Offset, out target);

        if (pointer.HasTargetSpan && pointer.HasTargetStreamSpan)
        {
            foreach (var value in _writtenPointerValues)
            {
                if (value.SourceOffset != pointer.TargetSpanOffset
                    || value.SourceLength != pointer.TargetSpanLength
                    || value.SourceStreamBlockIndex != pointer.TargetSpanStreamBlockIndex
                    || value.SourceStreamOffset != pointer.TargetSpanStreamOffset
                    || value.Address.BlockIndex != pointer.TargetSpanStreamBlockIndex)
                {
                    continue;
                }

                var delta = pointer.TargetOffset - pointer.TargetSpanStreamOffset;
                if (delta < 0 || delta >= value.WrittenLength)
                    return false;

                target = new XFileBlockAddress(value.Address.BlockIndex, value.Address.Offset + delta);
                return true;
            }

            return false;
        }

        return TryGetWrittenTargetBySourceStreamAddress(pointer.StreamBlockIndex, pointer.Offset, out target)
            || TryGetWrittenTargetBySourceStreamGap(pointer.StreamBlockIndex, pointer.Offset, out target);
    }

    private bool TryGetWrittenTargetBySourceStreamAddress(
        int sourceStreamBlockIndex,
        int sourceStreamOffset,
        out XFileBlockAddress target)
    {
        target = default;

        return _writtenPointerValuesBySourceStreamAddress.TryGetValue(
            new XFileBlockAddress(sourceStreamBlockIndex, sourceStreamOffset),
            out target);
    }

    private bool TryGetWrittenTargetBySourceStreamGap(
        int sourceStreamBlockIndex,
        int sourceStreamOffset,
        out XFileBlockAddress target)
    {
        target = default;
        WrittenPointerValueSpan? containingSpan = null;
        WrittenPointerValueSpan? previousSpan = null;
        WrittenPointerValueSpan? firstSpan = null;

        foreach (var value in _writtenPointerValues)
        {
            if (value.SourceStreamBlockIndex != sourceStreamBlockIndex
                || value.Address.BlockIndex != sourceStreamBlockIndex)
            {
                continue;
            }

            if (firstSpan is null || value.SourceStreamOffset < firstSpan.Value.SourceStreamOffset)
                firstSpan = value;

            var sourceEnd = value.SourceStreamOffset + value.SourceLength;
            if (sourceStreamOffset >= value.SourceStreamOffset && sourceStreamOffset < sourceEnd)
            {
                if (containingSpan is null || value.SourceLength < containingSpan.Value.SourceLength)
                    containingSpan = value;
                continue;
            }

            if (sourceEnd <= sourceStreamOffset
                && (previousSpan is null
                    || sourceEnd > previousSpan.Value.SourceStreamOffset + previousSpan.Value.SourceLength
                    || (sourceEnd == previousSpan.Value.SourceStreamOffset + previousSpan.Value.SourceLength
                        && value.Address.Offset + value.WrittenLength > previousSpan.Value.Address.Offset + previousSpan.Value.WrittenLength)))
            {
                previousSpan = value;
            }
        }

        if (containingSpan is { } containing)
        {
            var delta = sourceStreamOffset - containing.SourceStreamOffset;
            if (delta < 0 || delta >= containing.WrittenLength)
                return false;

            target = new XFileBlockAddress(sourceStreamBlockIndex, containing.Address.Offset + delta);
            return true;
        }

        if (previousSpan is { } previous)
        {
            var sourceEnd = previous.SourceStreamOffset + previous.SourceLength;
            var writtenEnd = previous.Address.Offset + previous.WrittenLength;
            target = new XFileBlockAddress(sourceStreamBlockIndex, sourceStreamOffset + writtenEnd - sourceEnd);
            return true;
        }

        if (firstSpan is { } first)
        {
            target = new XFileBlockAddress(
                sourceStreamBlockIndex,
                sourceStreamOffset + first.Address.Offset - first.SourceStreamOffset);
            return true;
        }

        return false;
    }

    private bool TryGetWrittenAliasCell(Pointer pointer, out XFileBlockAddress target)
    {
        target = default;
        if (pointer.Kind != PointerKind.Offset)
            return false;

        return _writtenAliasCellsBySource.TryGetValue(
            new XFileBlockAddress(pointer.StreamBlockIndex, pointer.Offset),
            out target);
    }

    private void WriteOffsetPointer(Pointer pointer)
    {
        var serializedOffset = SerializedPosition;
        WriteInt32(pointer.Raw);

        _pendingOffsetPointerFields.Add(new PendingOffsetPointerField(serializedOffset, pointer));
    }

    private readonly record struct WrittenPointerValueSpan(
        int SourceOffset,
        int SourceLength,
        int WrittenLength,
        int SourceStreamBlockIndex,
        int SourceStreamOffset,
        XFileBlockAddress Address);

    private readonly record struct PendingOffsetPointerField(
        int SerializedOffset,
        Pointer Pointer);

    private readonly record struct XFileStreamBlockStackEntry(
        int PreviousBlockIndex,
        int SavedActivePosition);
}
