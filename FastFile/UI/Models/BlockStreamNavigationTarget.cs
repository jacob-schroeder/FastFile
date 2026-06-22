using System;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;

namespace UI.Models;

public sealed class BlockStreamNavigationTarget(XBlockAddress address)
{
    public XFILE_BLOCK Block { get; } = address.Block;

    public int Offset { get; } = address.Offset;

    public string Label => $"{Block}:0x{Offset:X8}";

    public static BlockStreamNavigationTarget? FromPointer(Pointer? pointer)
    {
        return pointer is { Kind: PointerKind.Offset, Address: { } address } && address.Offset >= 0
            ? new BlockStreamNavigationTarget(address)
            : null;
    }

    public string ReplaceOffsetLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "[OFFSET]")
        {
            return Label;
        }

        const string marker = "([OFFSET])";
        var markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return value;
        }

        var hexStart = value.LastIndexOf("0x", markerIndex, StringComparison.OrdinalIgnoreCase);
        return hexStart < 0
            ? Label
            : value[..hexStart] + Label + value[(markerIndex + marker.Length)..];
    }
}
