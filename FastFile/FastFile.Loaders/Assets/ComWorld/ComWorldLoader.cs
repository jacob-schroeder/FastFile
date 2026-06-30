using FastFile.Models.Assets.ComWorld;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.ComWorld;

public sealed class ComWorldLoader
{
    public ComWorldAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level ComWorld pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            ComWorldAsset comWorld = ReadComWorld(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return comWorld;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static ComWorldAsset ReadComWorld(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, ComWorldAsset.SerializedSize, out XBlockAddress rootAddress);
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"ComWorld pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        int isInUse = rootCursor.ReadInt32();
        int primaryLightCount = rootCursor.ReadInt32();
        XPointer<ComPrimaryLight[]> primaryLightsPointer =
            context.PointerReader.ReadPointer<ComPrimaryLight[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != ComWorldAsset.SerializedSize)
            throw new InvalidDataException($"ComWorld consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ComWorldAsset.SerializedSize:X}.");

        if (primaryLightCount < 0 || primaryLightCount > 0x10000)
        {
            throw new InvalidDataException(
                $"ComWorld at source 0x{sourceOffset:X} has invalid primaryLightCount {primaryLightCount}; " +
                $"name=0x{namePointer.Raw:X8}, primaryLights=0x{primaryLightsPointer.Raw:X8}.");
        }

        context.Diagnostics.Trace(
            $"  ComWorld.header source=0x{sourceOffset:X}..0x{sourceOffset + ComWorldAsset.SerializedSize:X} " +
            $"root={rootAddress} name=0x{namePointer.Raw:X8}/{namePointer.Untyped.Type} isInUse={isInUse} " +
            $"primaryLightCount={primaryLightCount} primaryLights=0x{primaryLightsPointer.Raw:X8}/{primaryLightsPointer.Untyped.Type} " +
            $"bytes={PreviewBytes(rootBytes, 32)}");

        string? name;
        IReadOnlyList<ComPrimaryLight> primaryLights;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            primaryLights = ReadPrimaryLights(cursor, primaryLightsPointer.Untyped, primaryLightCount, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  ComWorld root source=0x{sourceOffset:X} name={name ?? "<null>"} isInUse={isInUse} " +
            $"primaryLightCount={primaryLightCount} primaryLights=0x{primaryLightsPointer.Raw:X8} " +
            $"loadedPrimaryLights={primaryLights.Count} blocks={context.Blocks.DescribePositions()}");

        return new ComWorldAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            IsInUse = isInUse,
            PrimaryLightCount = primaryLightCount,
            PrimaryLightsPointer = primaryLightsPointer,
            PrimaryLights = primaryLights
        };
    }

    private static IReadOnlyList<ComPrimaryLight> ReadPrimaryLights(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
        {
            if (count != 0)
                throw new InvalidDataException($"ComWorld primaryLights is null with non-zero count {count}.");

            return [];
        }

        if (pointer.Type == PointerType.Offset)
        {
            throw new InvalidDataException(
                $"ComWorld primaryLights pointer 0x{pointer.Raw:X8} is a packed offset pointer, but the PS3 ComWorld body only proves null/non-null inline array loading.");
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"ComWorld primaryLights pointer 0x{pointer.Raw:X8} is not inline/insert/null.");

        int sourceStart = cursor.Offset;
        int byteCount = checked(count * ComPrimaryLight.SerializedSize);
        XBlockAddress primaryLightsAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] lightBytes = context.Blocks.Load(
            cursor,
            byteCount,
            out XBlockAddress loadedAddress);
        if (loadedAddress != primaryLightsAddress)
            throw new InvalidDataException($"ComWorld primaryLights pointer patched to {primaryLightsAddress}, but array loaded at {loadedAddress}.");

        context.Diagnostics.Trace(
            $"    ComWorld.primaryLights.load source=0x{sourceStart:X}..0x{cursor.Offset:X} " +
            $"ptr=0x{pointer.Raw:X8}/{pointer.Type} count={count} stride=0x{ComPrimaryLight.SerializedSize:X} " +
            $"bytes=0x{byteCount:X} target={primaryLightsAddress} preview={PreviewBytes(lightBytes, 32)}");

        var lightCursor = new FastFileCursor(lightBytes, primaryLightsAddress);
        var primaryLights = new ComPrimaryLight[count];
        for (int i = 0; i < primaryLights.Length; i++)
        {
            int rowStart = lightCursor.Offset;
            XBlockAddress rowAddress = primaryLightsAddress.Add(rowStart);
            byte type = lightCursor.ReadByte();
            byte canUseShadowMap = lightCursor.ReadByte();
            byte exponent = lightCursor.ReadByte();
            byte unused = lightCursor.ReadByte();
            Vec3 color = ReadVec3(lightCursor);
            Vec3 dir = ReadVec3(lightCursor);
            Vec3 origin = ReadVec3(lightCursor);
            float radius = ReadSingle(lightCursor);
            float cosHalfFovOuter = ReadSingle(lightCursor);
            float cosHalfFovInner = ReadSingle(lightCursor);
            float cosHalfFovExpanded = ReadSingle(lightCursor);
            float rotationLimit = ReadSingle(lightCursor);
            float translationLimit = ReadSingle(lightCursor);
            XPointer<string> defNamePointer = context.PointerReader.ReadPointer<string>(lightCursor, XPointerResolutionMode.Direct);

            if (lightCursor.Offset - rowStart != ComPrimaryLight.SerializedSize)
                throw new InvalidDataException($"ComPrimaryLight consumed 0x{lightCursor.Offset - rowStart:X} bytes instead of 0x{ComPrimaryLight.SerializedSize:X}.");

            string? defName = context.PointerReader.LoadXString(cursor, defNamePointer);
            if (i < 4 || i == primaryLights.Length - 1)
            {
                context.Diagnostics.Trace(
                    $"      ComWorld.primaryLights[{i}] row={rowAddress} type={type} shadow={canUseShadowMap} exponent={exponent} " +
                    $"radius={radius:R} defName=0x{defNamePointer.Raw:X8}/{defNamePointer.Untyped.Type} value={defName ?? "<null>"}");
            }

            primaryLights[i] = new ComPrimaryLight
            {
                Offset = rowAddress.Offset,
                Type = type,
                CanUseShadowMap = canUseShadowMap,
                Exponent = exponent,
                Unused = unused,
                Color = color,
                Dir = dir,
                Origin = origin,
                Radius = radius,
                CosHalfFovOuter = cosHalfFovOuter,
                CosHalfFovInner = cosHalfFovInner,
                CosHalfFovExpanded = cosHalfFovExpanded,
                RotationLimit = rotationLimit,
                TranslationLimit = translationLimit,
                DefNamePointer = defNamePointer,
                DefName = defName
            };
        }

        context.Diagnostics.Trace(
            $"    ComWorld.primaryLights sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} " +
            $"target={primaryLightsAddress} count={primaryLights.Length} blocks={context.Blocks.DescribePositions()}");

        return primaryLights;
    }

    private static string PreviewBytes(ReadOnlySpan<byte> bytes, int maxBytes)
    {
        if (bytes.IsEmpty)
            return "<empty>";

        int headCount = Math.Min(bytes.Length, maxBytes);
        string head = Convert.ToHexString(bytes[..headCount]);
        if (bytes.Length <= maxBytes)
            return head;

        int tailCount = Math.Min(bytes.Length - headCount, maxBytes);
        string tail = Convert.ToHexString(bytes[^tailCount..]);
        return $"{head}...{tail}";
    }

    private static Vec3 ReadVec3(FastFileCursor cursor)
    {
        return new Vec3
        {
            X = ReadSingle(cursor),
            Y = ReadSingle(cursor),
            Z = ReadSingle(cursor)
        };
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }
}
