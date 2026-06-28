using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.Physics;
using FastFile.Models.Assets.Material;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XModelAssetModel = FastFile.Models.Assets.XModel.XModelAsset;
using XModelSurfsAssetModel = FastFile.Models.Assets.XModel.XModelSurfsAsset;
using PhysPresetAssetModel = FastFile.Models.Assets.XModel.PhysPresetAsset;
using PhysCollmapAssetModel = FastFile.Models.Assets.Physics.PhysCollmapAsset;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.XModel;

public sealed class XModelLoader
{
    private const int XModelSize = 0x120;
    private const int XModelLodInfoSize = 0x28;
    private const int XModelSurfsSize = 0x24;
    private const int XSurfaceSize = 0x54;
    private const int XRigidVertListSize = 0x0c;
    private const int XSurfaceCollisionTreeSize = 0x28;
    private const int XSurfaceCollisionNodeSize = 0x10;
    private const int XSurfaceCollisionLeafSize = 0x02;
    private const int PhysPresetSize = 0x2c;
    private const int PhysCollmapSize = 0x48;

    private readonly MaterialLoader _materialLoader = new();
    private readonly PhysCollmapLoader _physCollmapLoader = new();

    public XModelAssetModel LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level XModel pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        return LoadInlineOrInsertXModel(cursor, pointer, context);
    }

    public XModelAssetModel? LoadFromPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, XModelSize, "XModel"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, XModelSize, "XModel");
            return null;
        }

        return LoadInlineOrInsertXModel(cursor, pointer, context);
    }

    private XModelAssetModel LoadInlineOrInsertXModel(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XModelAssetModel model = ReadInlineXModel(cursor, pointer, context, out XBlockAddress rootAddress);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

        return model;
    }

    private XModelAssetModel ReadInlineXModel(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context,
        out XBlockAddress rootAddress)
    {
        int sourceOffset = cursor.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, XModelSize, out rootAddress);
            if (rootAddress != targetAddress)
                throw new InvalidDataException($"XModel pointer patched to {targetAddress}, but root loaded at {rootAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            XString namePointer = ReadXStringPointer(rootCursor);
            byte numBones = rootCursor.ReadByte();
            byte numRootBones = rootCursor.ReadByte();
            byte numSurfs = rootCursor.ReadByte();
            rootCursor.Skip(1);
            rootCursor.Skip(0x24 - rootCursor.Offset);
            XPointer<ushort[]> boneNamesPointer = ReadPointer<ushort[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> parentListPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<short[]> quatsPointer = ReadPointer<short[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<float[]> transPointer = ReadPointer<float[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> partClassificationPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> baseMatPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<XPointer<MaterialAsset>[]> materialHandlesPointer = ReadPointer<XPointer<MaterialAsset>[]>(rootCursor, XPointerResolutionMode.Direct);

            rootCursor.Skip(0xe4 - rootCursor.Offset);
            XPointer<byte[]> collSurfsPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            int numCollSurfs = rootCursor.ReadInt32();
            rootCursor.Skip(0xf0 - rootCursor.Offset);
            XPointer<byte[]> boneInfoPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            rootCursor.Skip(0x110 - rootCursor.Offset);
            XPointer<ushort[]> invHighMipRadiusPointer = ReadPointer<ushort[]>(rootCursor, XPointerResolutionMode.Direct);
            rootCursor.Skip(0x118 - rootCursor.Offset);
            XPointerReference physPresetPointer = ReadPointer<PhysPresetAssetModel>(rootCursor, XPointerResolutionMode.AliasCell).Untyped;
            XPointerReference physCollmapPointer = ReadPointer<PhysCollmapAssetModel>(rootCursor, XPointerResolutionMode.AliasCell).Untyped;

            if (rootCursor.Offset != XModelSize)
                throw new InvalidDataException($"XModel consumed 0x{rootCursor.Offset:X} bytes instead of 0x{XModelSize:X}.");

            int partCount = Math.Max(0, numBones - numRootBones);
            string? name;

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                name = ReadXString(cursor, namePointer, context);
                ReadUInt16Array(cursor, boneNamesPointer.Untyped, numBones, context);
                ReadByteArray(cursor, parentListPointer.Untyped, partCount, context);
                ReadInt16Array(cursor, quatsPointer.Untyped, partCount * 4, context);
                ReadFloatArray(cursor, transPointer.Untyped, partCount * 3, context);
                ReadByteArray(cursor, partClassificationPointer.Untyped, numBones, context);
                ReadRawBytes(cursor, baseMatPointer.Untyped, numBones * 0x20, alignment: 4, context);
                IReadOnlyList<XPointer<MaterialAsset>> materialPointers =
                    ReadAliasPointerArrayPayload<MaterialAsset>(cursor, materialHandlesPointer.Untyped, numSurfs, context);
                ReadMaterialPointers(cursor, materialPointers, context);

                for (int i = 0; i < 4; i++)
                {
                    int lodOffset = 0x40 + (i * XModelLodInfoSize);
                    var lodCursor = new FastFileCursor(rootBytes.AsSpan(lodOffset, XModelLodInfoSize).ToArray(), rootAddress with { Offset = rootAddress.Offset + lodOffset });
                    lodCursor.Skip(0x04);
                    ushort lodNumSurfs = lodCursor.ReadUInt16();
                    lodCursor.Skip(0x08 - lodCursor.Offset);
                    XPointerReference modelSurfsPointer = ReadPointer<XModelSurfsAssetModel>(lodCursor, XPointerResolutionMode.AliasCell).Untyped;
                    ReadXModelSurfsPointer(cursor, modelSurfsPointer, lodNumSurfs, context);
                }

                ReadRawBytes(cursor, collSurfsPointer.Untyped, checked(numCollSurfs * 0x24), alignment: 4, context);
                ReadRawBytes(cursor, boneInfoPointer.Untyped, checked(numBones * 0x1c), alignment: 4, context);
                ReadUInt16Array(cursor, invHighMipRadiusPointer.Untyped, numSurfs, context);
                ReadPhysPresetPointer(cursor, physPresetPointer, context);
                _physCollmapLoader.LoadFromPointer(cursor, physCollmapPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"      XModel root source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} bones={numBones}/{numRootBones} " +
                $"surfs={numSurfs} physPreset=0x{physPresetPointer.Raw:X8} physCollmap=0x{physCollmapPointer.Raw:X8} " +
                $"blocks={context.Blocks.DescribePositions()}");

            return new XModelAssetModel
            {
                Offset = sourceOffset,
                NamePointer = namePointer,
                Name = name,
                NumBones = numBones,
                NumRootBones = numRootBones,
                NumSurfs = numSurfs
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private void ReadXModelSurfsPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        ushort lodNumSurfs,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, XModelSurfsSize, "XModelSurfs"))
            return;

        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, XModelSurfsSize, "XModelSurfs");
            return;
        }

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, XModelSurfsSize, out XBlockAddress loadedAddress);
            if (loadedAddress != rootAddress)
                throw new InvalidDataException($"XModelSurfs pointer patched to {rootAddress}, but root loaded at {loadedAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor);
            XPointer<byte[]> surfsPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            ushort numSurfs = rootCursor.ReadUInt16();
            rootCursor.Skip(XModelSurfsSize - rootCursor.Offset);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadXSurfaceArray(cursor, surfsPointer.Untyped, numSurfs == 0 ? lodNumSurfs : numSurfs, context);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private void ReadXSurfaceArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0 || pointer.Type == PointerType.Null)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * XSurfaceSize), "XSurface[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] surfaceBytes = context.Blocks.Load(cursor, checked(count * XSurfaceSize), out XBlockAddress arrayAddress);

        for (int i = 0; i < count; i++)
        {
            int offset = i * XSurfaceSize;
            var surfaceCursor = new FastFileCursor(surfaceBytes.AsSpan(offset, XSurfaceSize).ToArray(), arrayAddress with { Offset = arrayAddress.Offset + offset });
            ReadXSurfaceChildren(cursor, surfaceCursor, context);
        }
    }

    private void ReadXSurfaceChildren(
        FastFileCursor cursor,
        FastFileCursor surfaceCursor,
        FastFileLoadContext context)
    {
        surfaceCursor.Skip(0x02);
        byte streamFlags = surfaceCursor.ReadByte();
        surfaceCursor.Skip(0x04 - surfaceCursor.Offset);
        ushort vertCount = surfaceCursor.ReadUInt16();
        ushort triCount = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> triIndicesPointer = ReadPointer<ushort[]>(surfaceCursor, XPointerResolutionMode.Direct);
        ushort blend0 = surfaceCursor.ReadUInt16();
        ushort blend1 = surfaceCursor.ReadUInt16();
        ushort blend2 = surfaceCursor.ReadUInt16();
        ushort blend3 = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> vertsBlendPointer = ReadPointer<ushort[]>(surfaceCursor, XPointerResolutionMode.Direct);
        XPointer<byte[]> verts0Pointer = ReadPointer<byte[]>(surfaceCursor, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(0x24 - surfaceCursor.Offset);
        XPointer<byte[]> verts1Pointer = ReadPointer<byte[]>(surfaceCursor, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(0x30 - surfaceCursor.Offset);
        int vertListCount = surfaceCursor.ReadInt32();
        XPointer<byte[]> vertListPointer = ReadPointer<byte[]>(surfaceCursor, XPointerResolutionMode.Direct);
        surfaceCursor.Skip(XSurfaceSize - surfaceCursor.Offset);

        int blendCount = blend0 + (blend1 * 3) + (blend2 * 5) + (blend3 * 7);

        ReadRawBytes(cursor, vertsBlendPointer.Untyped, checked(blendCount * sizeof(ushort)), alignment: 2, context);
        ReadSurfaceStreamBytes(cursor, verts0Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushPhysical: (streamFlags & 0x01) == 0, context);
        ReadSurfaceStreamBytes(cursor, verts1Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushPhysical: (streamFlags & 0x02) == 0, context);
        ReadRigidVertListArray(cursor, vertListPointer.Untyped, vertListCount, context);
        ReadSurfaceStreamBytes(cursor, triIndicesPointer.Untyped, checked(triCount * 3 * sizeof(ushort)), alignment: 16, pushPhysical: (streamFlags & 0x04) == 0, context);
    }

    private void ReadRigidVertListArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0 || pointer.Type == PointerType.Null)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * XRigidVertListSize), "XRigidVertList[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] listBytes = context.Blocks.Load(cursor, checked(count * XRigidVertListSize), out XBlockAddress listAddress);
        for (int i = 0; i < count; i++)
        {
            int offset = i * XRigidVertListSize;
            var listCursor = new FastFileCursor(listBytes.AsSpan(offset, XRigidVertListSize).ToArray(), listAddress with { Offset = listAddress.Offset + offset });
            listCursor.Skip(0x08);
            XPointer<byte[]> collisionTreePointer = ReadPointer<byte[]>(listCursor, XPointerResolutionMode.Direct);
            ReadXSurfaceCollisionTree(cursor, collisionTreePointer.Untyped, context);
        }
    }

    private void ReadXSurfaceCollisionTree(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, XSurfaceCollisionTreeSize, "XSurfaceCollisionTree");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] treeBytes = context.Blocks.Load(cursor, XSurfaceCollisionTreeSize, out XBlockAddress treeAddress);
        var treeCursor = new FastFileCursor(treeBytes, treeAddress);
        treeCursor.Skip(0x18);
        int nodeCount = treeCursor.ReadInt32();
        XPointer<byte[]> nodesPointer = ReadPointer<byte[]>(treeCursor, XPointerResolutionMode.Direct);
        int leafCount = treeCursor.ReadInt32();
        XPointer<byte[]> leafsPointer = ReadPointer<byte[]>(treeCursor, XPointerResolutionMode.Direct);

        ReadRawBytes(cursor, nodesPointer.Untyped, checked(nodeCount * XSurfaceCollisionNodeSize), alignment: 16, context);
        ReadRawBytes(cursor, leafsPointer.Untyped, checked(leafCount * XSurfaceCollisionLeafSize), alignment: 2, context);
    }

    private void ReadPhysPresetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, PhysPresetSize, "PhysPreset"))
            return;

        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, PhysPresetSize, "PhysPreset");
            return;
        }

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, PhysPresetSize);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor);
            rootCursor.Skip(0x1c - rootCursor.Offset);
            XString sndAliasPrefixPointer = ReadXStringPointer(rootCursor);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadXString(cursor, sndAliasPrefixPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private void ReadMaterialPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<MaterialAsset>> pointers,
        FastFileLoadContext context)
    {
        foreach (XPointer<MaterialAsset> pointer in pointers)
            ReadMaterialPointer(cursor, pointer.Untyped, context);
    }

    private void ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, MaterialAsset.SerializedSize, "Material"))
            return;

        _materialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private void ReadSurfaceStreamBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        bool pushPhysical,
        FastFileLoadContext context)
    {
        if (!pushPhysical)
        {
            ReadRawBytes(cursor, pointer, byteCount, alignment, context);
            return;
        }

        context.Blocks.Push(XFileBlockType.PHYSICAL);
        try
        {
            ReadRawBytes(cursor, pointer, byteCount, alignment, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static IReadOnlyList<XPointer<T>> ReadAliasPointerArrayPayload<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative alias pointer array count {count}.");

        int byteCount = checked(count * sizeof(int));
        if (pointer.Type == PointerType.Null)
            return [];

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, $"{typeof(T).Name}*[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var pointers = new XPointer<T>[count];

        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadPointer<T>(pointerCursor, XPointerResolutionMode.AliasCell);

        return pointers;
    }

    private static void ReadByteArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, count, alignment: 1, context);
    }

    private static void ReadInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, checked(count * sizeof(short)), alignment: 2, context);
    }

    private static void ReadUInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, checked(count * sizeof(ushort)), alignment: 2, context);
    }

    private static void ReadFloatArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        ReadRawBytes(cursor, pointer, checked(count * sizeof(float)), alignment: 4, context);
    }

    private static void ReadRawBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Invalid negative byte count {byteCount}.");

        if (pointer.Type == PointerType.Null)
            return;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "byte[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        context.Blocks.Load(cursor, byteCount);
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

    private static string? ReadXString(
        FastFileCursor cursor,
        XString pointer,
        FastFileLoadContext context)
    {
        return context.PointerReader.LoadXString(cursor, pointer);
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
}
