using FastFile.Models.Assets.Image;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.Image;

public sealed class GfxImageLoader
{
    public GfxImageAsset? LoadFromPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, GfxImageAsset.SerializedSize, "GfxImage"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, GfxImageAsset.SerializedSize, "GfxImage");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new NotSupportedException($"GfxImage pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        int sourceOffset = cursor.Offset;
        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, GfxImageAsset.SerializedSize, out XBlockAddress loadedAddress);
            if (loadedAddress != rootAddress)
                throw new InvalidDataException($"GfxImage pointer patched to {rootAddress}, but root loaded at {loadedAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            byte format = rootCursor.ReadByte();
            byte levelCount = rootCursor.ReadByte();
            byte unknown02 = rootCursor.ReadByte();
            byte multiFaceControl = rootCursor.ReadByte();
            uint textureFlags = rootCursor.ReadUInt32();
            ushort width = rootCursor.ReadUInt16();
            ushort height = rootCursor.ReadUInt16();
            ushort depth = rootCursor.ReadUInt16();
            rootCursor.Skip(0x18 - rootCursor.Offset);
            byte mapType = rootCursor.ReadByte();
            byte textureSemantic = rootCursor.ReadByte();
            rootCursor.Skip(0x28 - rootCursor.Offset);
            XPointerReference payloadPointer = ReadRawCell(rootCursor, XPointerOffsetMode.Direct);
            rootCursor.Skip(0x4c - rootCursor.Offset);
            XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);

            if (rootCursor.Offset != GfxImageAsset.SerializedSize)
                throw new InvalidDataException($"GfxImage consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GfxImageAsset.SerializedSize:X}.");

            string? name;
            int payloadByteCount;
            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                name = context.PointerReader.LoadXString(cursor, namePointer);
                payloadByteCount = ReadPayload(
                    cursor,
                    payloadPointer,
                    format,
                    levelCount,
                    multiFaceControl,
                    textureFlags,
                    width,
                    height,
                    depth,
                    textureSemantic,
                    context);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            context.Diagnostics.Trace(
                $"      GfxImage root source=0x{sourceOffset:X} root={rootAddress} format=0x{format:X2} flags=0x{textureFlags:X8} " +
                $"dims={width}x{height}x{depth} levels={levelCount} map=0x{mapType:X2} semantic=0x{textureSemantic:X2} " +
                $"payload=0x{payloadPointer.Raw:X8} payloadBytes=0x{payloadByteCount:X} name={name ?? "<null>"} blocks={context.Blocks.DescribePositions()}");

            return new GfxImageAsset
            {
                Offset = sourceOffset,
                RootBytes = rootBytes,
                Format = format,
                LevelCount = levelCount,
                Unknown02 = unknown02,
                MultiFaceControl = multiFaceControl,
                TextureFlags = textureFlags,
                Width = width,
                Height = height,
                Depth = depth,
                MapType = mapType,
                TextureSemantic = textureSemantic,
                PayloadPointer = payloadPointer,
                PayloadByteCount = payloadByteCount,
                NamePointer = namePointer,
                Name = name
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static int ReadPayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        byte format,
        byte levelCount,
        byte multiFaceControl,
        uint textureFlags,
        ushort width,
        ushort height,
        ushort depth,
        byte textureSemantic,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return 0;

        int byteCount = ComputePayloadByteCount(format, levelCount, multiFaceControl, textureFlags, width, height, depth);

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "GfxImage payload");
            return byteCount;
        }

        if (pointer.Type is not PointerType.Inline)
            throw new NotSupportedException($"GfxImage payload pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"GfxImage payload pointer 0x{pointer.Raw:X8} has no destination cell address.");

        XFileBlockType payloadBlock = textureSemantic == 0x0b
            ? XFileBlockType.RUNTIME
            : XFileBlockType.PHYSICAL;

        context.Blocks.Push(payloadBlock);
        try
        {
            context.Blocks.AlignCurrent(128);
            XBlockAddress payloadAddress = context.Blocks.CurrentAddress;
            context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(payloadAddress));
            context.Blocks.Load(cursor, byteCount);
            context.Diagnostics.Trace(
                $"        GfxImage payload block={payloadBlock} target={payloadAddress} bytes=0x{byteCount:X} blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }

        return byteCount;
    }

    private static int ComputePayloadByteCount(
        byte format,
        byte levelCount,
        byte multiFaceControl,
        uint textureFlags,
        ushort width,
        ushort height,
        ushort depth)
    {
        byte normalizedFormat = (byte)(format & 0xdf);
        uint formatKey = (textureFlags << 8) | normalizedFormat;
        long total = 0;

        for (int level = 0; level < levelCount; level++)
        {
            int levelWidth = Math.Max(1, width >> level);
            int levelHeight = Math.Max(1, height >> level);
            int levelDepth = Math.Max(1, depth >> level);
            total += ComputeMipByteCount(formatKey, levelWidth, levelHeight, levelDepth);
        }

        total = Align(total, 128);
        if (multiFaceControl != 0)
            total = Align(checked(total * 6), 128);

        if (total > int.MaxValue)
            throw new InvalidDataException($"GfxImage payload size 0x{total:X} does not fit in this loader.");

        return (int)total;
    }

    private static long ComputeMipByteCount(uint formatKey, int width, int height, int depth)
    {
        return formatKey switch
        {
            0x01AAE485 or
            0x01AAE490 or
            0x01AAE49C or
            0x01AAE49E or
            0x00AAFE9F => checked((long)width * height * depth * 4),

            0x01AAE492 or
            0x01AAAB8B => checked((long)width * height * depth * 2),

            0x01A9FF81 or
            0x0156FF81 => checked((long)width * height * depth),

            0x01A9AA86 or
            0x01AA5686 or
            0x0156AA86 or
            0x01AAE486 => checked((long)((width + 3) >> 2) * ((height + 3) >> 2) * depth * 8),

            0x01AAE487 or
            0x01AAE488 => checked((long)((width + 3) >> 2) * ((height + 3) >> 2) * depth * 16),

            _ => throw new NotSupportedException(
                $"Unsupported GfxImage format key 0x{formatKey:X8} for {width}x{height}x{depth} mip payload.")
        };
    }

    private static long Align(long value, int alignment)
    {
        return checked((value + alignment - 1) / alignment * alignment);
    }

    private static XPointerReference ReadRawCell(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode)
    {
        int cellOffset = cursor.Offset;
        return XPointerReference.FromRaw(
            cursor.ReadInt32(),
            offsetMode,
            cursor.AddressAt(cellOffset));
    }

    private static bool ResolveAliasCellOffset(
        XPointerReference pointer,
        FastFileLoadContext context,
        int targetByteCount,
        string targetName)
    {
        if (pointer.Type != PointerType.Offset || pointer.ResolutionMode != XPointerResolutionMode.AliasCell)
            return false;

        if (pointer.CellAddress is not { } destinationCell)
            throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} has no destination cell to patch.");

        int aliasedRaw = context.PointerReader.ReadAliasCellRaw(pointer);
        if (aliasedRaw != 0)
        {
            PointerType aliasedType = XPointerCodec.GetType(aliasedRaw);
            if (aliasedType != PointerType.Offset)
                throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} resolved to unresolved sentinel 0x{aliasedRaw:X8} for {targetName}.");

            context.PointerReader.ValidateOffsetPointerRange(
                XPointerReference.FromRaw(aliasedRaw, XPointerResolutionMode.Direct, pointer.PackedAddress),
                targetByteCount,
                targetName);
        }

        context.Blocks.WriteInt32(destinationCell, aliasedRaw);
        return true;
    }
}
