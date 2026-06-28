using FastFile.Models.Assets.Physics;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Physics;

public sealed class PhysCollmapLoader
{
    public PhysCollmapAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level PhysCollmap pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        return LoadInlineOrInsert(cursor, pointer, context);
    }

    public PhysCollmapAsset? LoadFromPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, PhysCollmapAsset.SerializedSize, "PhysCollmap"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, PhysCollmapAsset.SerializedSize, "PhysCollmap");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new NotSupportedException($"PhysCollmap pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        return LoadInlineOrInsert(cursor, pointer, context);
    }

    private static PhysCollmapAsset LoadInlineOrInsert(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            PhysCollmapAsset asset = ReadPhysCollmap(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return asset;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static PhysCollmapAsset ReadPhysCollmap(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, PhysCollmapAsset.SerializedSize, out XBlockAddress rootAddress);
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"PhysCollmap pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XString namePointer = ReadXStringPointer(rootCursor);
        int count = rootCursor.ReadInt32();
        XPointer<PhysGeomInfo[]> geomsPointer = ReadPointer<PhysGeomInfo[]>(rootCursor, XPointerResolutionMode.Direct);
        PhysMass mass = ReadPhysMass(rootCursor);
        Bounds bounds = ReadBounds(rootCursor);

        if (rootCursor.Offset != PhysCollmapAsset.SerializedSize)
            throw new InvalidDataException($"PhysCollmap consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PhysCollmapAsset.SerializedSize:X}.");

        string? name;
        IReadOnlyList<PhysGeomInfo> geoms;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            geoms = ReadPhysGeomInfoArray(cursor, geomsPointer.Untyped, count, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"      PhysCollmap root source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} count={count} " +
            $"geoms=0x{geomsPointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        return new PhysCollmapAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            Count = count,
            GeomsPointer = geomsPointer,
            Geoms = geoms,
            Mass = mass,
            Bounds = bounds
        };
    }

    private static IReadOnlyList<PhysGeomInfo> ReadPhysGeomInfoArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative PhysGeomInfo count {count}.");

        if (pointer.Type == PointerType.Null)
            return [];

        XBlockAddress address = PatchCurrentPointerCell(pointer, alignment: 4, checked(count * PhysGeomInfo.SerializedSize), "PhysGeomInfo[]", context);
        if (pointer.Type == PointerType.Offset || count == 0)
            return [];

        byte[] bytes = context.Blocks.Load(cursor, checked(count * PhysGeomInfo.SerializedSize));
        var geoms = new PhysGeomInfo[count];
        var brushPointers = new XPointer<BrushWrapper>[count];

        for (int i = 0; i < count; i++)
        {
            int entryOffset = i * PhysGeomInfo.SerializedSize;
            var entryCursor = new FastFileCursor(
                bytes.AsSpan(entryOffset, PhysGeomInfo.SerializedSize).ToArray(),
                address with { Offset = address.Offset + entryOffset });

            XPointer<BrushWrapper> brushPointer = ReadPointer<BrushWrapper>(entryCursor, XPointerResolutionMode.Direct);
            brushPointers[i] = brushPointer;
            int type = entryCursor.ReadInt32();
            Vec3[] orientation = [ReadVec3(entryCursor), ReadVec3(entryCursor), ReadVec3(entryCursor)];
            Bounds bounds = ReadBounds(entryCursor);

            geoms[i] = new PhysGeomInfo
            {
                BrushWrapperPointer = brushPointer,
                Type = type,
                Orientation = orientation,
                Bounds = bounds
            };
        }

        for (int i = 0; i < geoms.Length; i++)
        {
            geoms[i] = new PhysGeomInfo
            {
                BrushWrapperPointer = geoms[i].BrushWrapperPointer,
                BrushWrapper = ReadBrushWrapper(cursor, brushPointers[i].Untyped, context),
                Type = geoms[i].Type,
                Orientation = geoms[i].Orientation,
                Bounds = geoms[i].Bounds
            };
        }

        return geoms;
    }

    private static BrushWrapper? ReadBrushWrapper(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        XBlockAddress address = PatchCurrentPointerCell(pointer, alignment: 4, BrushWrapper.SerializedSize, "BrushWrapper", context);
        if (pointer.Type == PointerType.Offset)
            return null;

        byte[] bytes = context.Blocks.Load(cursor, BrushWrapper.SerializedSize);
        var wrapperCursor = new FastFileCursor(bytes, address);
        Bounds bounds = ReadBounds(wrapperCursor);

        var brushCursor = new FastFileCursor(
            bytes.AsSpan(0x18, CBrush.SerializedSize).ToArray(),
            address with { Offset = address.Offset + 0x18 });
        CBrush brushRoot = ReadCBrushRoot(brushCursor);
        wrapperCursor.Skip(0x3c - wrapperCursor.Offset);
        int totalEdgeCount = wrapperCursor.ReadInt32();
        XPointer<CPlane[]> planesPointer = ReadPointer<CPlane[]>(wrapperCursor, XPointerResolutionMode.Direct);

        IReadOnlyList<CBrushSide> sides = ReadCBrushSideArray(cursor, brushRoot.SidesPointer.Untyped, brushRoot.NumSides, context);
        IReadOnlyList<byte> baseAdjacentSide = ReadByteArray(cursor, brushRoot.BaseAdjacentSidePointer.Untyped, totalEdgeCount, context);
        CBrush brush = new()
        {
            NumSides = brushRoot.NumSides,
            GlassPieceIndex = brushRoot.GlassPieceIndex,
            SidesPointer = brushRoot.SidesPointer,
            Sides = sides,
            BaseAdjacentSidePointer = brushRoot.BaseAdjacentSidePointer,
            BaseAdjacentSide = baseAdjacentSide,
            AxialMaterialNum = brushRoot.AxialMaterialNum,
            FirstAdjacentSideOffsets = brushRoot.FirstAdjacentSideOffsets,
            EdgeCount = brushRoot.EdgeCount
        };

        IReadOnlyList<CPlane> planes = ReadCPlaneArray(cursor, planesPointer.Untyped, brush.NumSides, context);
        return new BrushWrapper
        {
            Bounds = bounds,
            Brush = brush,
            TotalEdgeCount = totalEdgeCount,
            PlanesPointer = planesPointer,
            Planes = planes
        };
    }

    private static CBrush ReadCBrushRoot(FastFileCursor cursor)
    {
        ushort numSides = cursor.ReadUInt16();
        ushort glassPieceIndex = cursor.ReadUInt16();
        XPointer<CBrushSide[]> sidesPointer = ReadPointer<CBrushSide[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<byte[]> baseAdjacentSidePointer = ReadPointer<byte[]>(cursor, XPointerResolutionMode.Direct);
        var axialMaterialNum = new short[6];
        for (int i = 0; i < axialMaterialNum.Length; i++)
            axialMaterialNum[i] = unchecked((short)cursor.ReadUInt16());

        byte[] firstAdjacentSideOffsets = cursor.ReadBytes(6);
        byte[] edgeCount = cursor.ReadBytes(6);
        return new CBrush
        {
            NumSides = numSides,
            GlassPieceIndex = glassPieceIndex,
            SidesPointer = sidesPointer,
            BaseAdjacentSidePointer = baseAdjacentSidePointer,
            AxialMaterialNum = axialMaterialNum,
            FirstAdjacentSideOffsets = firstAdjacentSideOffsets,
            EdgeCount = edgeCount
        };
    }

    private static IReadOnlyList<CBrushSide> ReadCBrushSideArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return [];

        XBlockAddress address = PatchCurrentPointerCell(pointer, alignment: 4, checked(count * CBrushSide.SerializedSize), "CBrushSide[]", context);
        if (pointer.Type == PointerType.Offset || count == 0)
            return [];

        byte[] bytes = context.Blocks.Load(cursor, checked(count * CBrushSide.SerializedSize));
        var sides = new CBrushSide[count];
        for (int i = 0; i < sides.Length; i++)
        {
            int entryOffset = i * CBrushSide.SerializedSize;
            var entryCursor = new FastFileCursor(
                bytes.AsSpan(entryOffset, CBrushSide.SerializedSize).ToArray(),
                address with { Offset = address.Offset + entryOffset });

            XPointer<CPlane> planePointer = ReadPointer<CPlane>(entryCursor, XPointerResolutionMode.Direct);
            sides[i] = new CBrushSide
            {
                PlanePointer = planePointer,
                Plane = ReadCPlanePointer(cursor, planePointer.Untyped, context),
                MaterialNum = entryCursor.ReadUInt16(),
                FirstAdjacentSideOffset = entryCursor.ReadByte(),
                EdgeCount = entryCursor.ReadByte()
            };
        }

        return sides;
    }

    private static IReadOnlyList<CPlane> ReadCPlaneArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return [];

        XBlockAddress address = PatchCurrentPointerCell(pointer, alignment: 4, checked(count * CPlane.SerializedSize), "CPlane[]", context);
        if (pointer.Type == PointerType.Offset || count == 0)
            return [];

        byte[] bytes = context.Blocks.Load(cursor, checked(count * CPlane.SerializedSize));
        var planes = new CPlane[count];
        for (int i = 0; i < planes.Length; i++)
        {
            int entryOffset = i * CPlane.SerializedSize;
            var entryCursor = new FastFileCursor(
                bytes.AsSpan(entryOffset, CPlane.SerializedSize).ToArray(),
                address with { Offset = address.Offset + entryOffset });
            planes[i] = ReadCPlane(entryCursor);
        }

        return planes;
    }

    private static CPlane? ReadCPlanePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        XBlockAddress address = PatchCurrentPointerCell(pointer, alignment: 4, CPlane.SerializedSize, "CPlane", context);
        if (pointer.Type == PointerType.Offset)
            return null;

        byte[] bytes = context.Blocks.Load(cursor, CPlane.SerializedSize);
        return ReadCPlane(new FastFileCursor(bytes, address));
    }

    private static CPlane ReadCPlane(FastFileCursor cursor)
    {
        Vec3 normal = ReadVec3(cursor);
        float dist = ReadSingle(cursor);
        byte type = cursor.ReadByte();
        byte signBits = cursor.ReadByte();
        byte[] pad12 = cursor.ReadBytes(2);
        return new CPlane
        {
            Normal = normal,
            Dist = dist,
            Type = type,
            SignBits = signBits,
            Pad12 = pad12
        };
    }

    private static IReadOnlyList<byte> ReadByteArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative byte count {count}.");

        if (pointer.Type == PointerType.Null)
            return [];

        PatchCurrentPointerCell(pointer, alignment: 1, count, "byte[]", context);
        if (pointer.Type == PointerType.Offset || count == 0)
            return [];

        return context.Blocks.Load(cursor, count);
    }

    private static XBlockAddress PatchCurrentPointerCell(
        XPointerReference pointer,
        int alignment,
        int byteCount,
        string targetName,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, targetName);
            return pointer.PackedAddress ?? throw new InvalidDataException($"Offset pointer 0x{pointer.Raw:X8} has no packed address for {targetName}.");
        }

        if (pointer.Type != PointerType.Inline)
            throw new NotSupportedException($"PhysCollmap {targetName} pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        return context.PointerReader.PatchInlinePointerCell(pointer, alignment);
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
            if (XPointerCodec.GetType(aliasedRaw) != PointerType.Offset)
                throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} resolved to unresolved sentinel 0x{aliasedRaw:X8} for {targetName}.");

            context.PointerReader.ValidateOffsetPointerRange(
                XPointerReference.FromRaw(aliasedRaw, XPointerResolutionMode.Direct, pointer.PackedAddress),
                targetByteCount,
                targetName);
        }

        context.Blocks.WriteInt32(destinationCell, aliasedRaw);
        return true;
    }

    private static PhysMass ReadPhysMass(FastFileCursor cursor)
    {
        return new PhysMass
        {
            CenterOfMass = ReadVec3(cursor),
            MomentsOfInertia = ReadVec3(cursor),
            ProductsOfInertia = ReadVec3(cursor)
        };
    }

    private static Bounds ReadBounds(FastFileCursor cursor)
    {
        return new Bounds
        {
            MidPoint = ReadVec3(cursor),
            HalfSize = ReadVec3(cursor)
        };
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

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        XPointerResolutionMode mode)
    {
        int cellOffset = cursor.Offset;
        return new XPointer<T>(cursor.ReadInt32(), mode, cursor.AddressAt(cellOffset));
    }

    private static XString ReadXStringPointer(FastFileCursor cursor)
    {
        return ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }
}
