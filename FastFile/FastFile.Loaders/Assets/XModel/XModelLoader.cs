using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.Physics;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.XModel;
using ModelBounds = FastFile.Models.Math.Bounds;
using ModelVec3 = FastFile.Models.Math.Vec3;
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
        if (ResolveAliasCellOffset<XModelAssetModel>(pointer, context, XModelSize, "XModel"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<XModelAssetModel>(pointer, XModelSize, "XModel");
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
            byte pad07 = rootCursor.ReadByte();
            float scale = ReadSingle(rootCursor);
            IReadOnlyList<uint> noScalePartBits = ReadUInt32Values(rootCursor, 6);
            XPointer<ushort[]> boneNamesPointer = ReadPointer<ushort[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> parentListPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<short[]> quatsPointer = ReadPointer<short[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<float[]> transPointer = ReadPointer<float[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> partClassificationPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<byte[]> baseMatPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            XPointer<XPointer<MaterialAsset>[]> materialHandlesPointer = ReadPointer<XPointer<MaterialAsset>[]>(rootCursor, XPointerResolutionMode.Direct);

            rootCursor.Skip(0xe0 - rootCursor.Offset);
            byte maxLoadedLod = rootCursor.ReadByte();
            byte numLods = rootCursor.ReadByte();
            byte collLod = rootCursor.ReadByte();
            byte flags = rootCursor.ReadByte();
            XPointer<byte[]> collSurfsPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            int numCollSurfs = rootCursor.ReadInt32();
            int contents = rootCursor.ReadInt32();
            XPointer<byte[]> boneInfoPointer = ReadPointer<byte[]>(rootCursor, XPointerResolutionMode.Direct);
            float radius = ReadSingle(rootCursor);
            ModelBounds bounds = ReadBounds(rootCursor);
            XPointer<ushort[]> invHighMipRadiusPointer = ReadPointer<ushort[]>(rootCursor, XPointerResolutionMode.Direct);
            int memUsage = rootCursor.ReadInt32();
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
                IReadOnlyList<ushort> boneNames = ReadUInt16Array(cursor, boneNamesPointer.Untyped, numBones, context);
                IReadOnlyList<byte> parentList = ReadByteArray(cursor, parentListPointer.Untyped, partCount, context);
                IReadOnlyList<short> quats = ReadInt16Array(cursor, quatsPointer.Untyped, partCount * 4, context);
                IReadOnlyList<float> trans = ReadFloatArray(cursor, transPointer.Untyped, partCount * 3, context);
                IReadOnlyList<byte> partClassification = ReadByteArray(cursor, partClassificationPointer.Untyped, numBones, context);
                IReadOnlyList<DObjAnimMat> baseMat = ReadDObjAnimMatArray(cursor, baseMatPointer.Untyped, numBones, context);
                IReadOnlyList<XPointer<MaterialAsset>> materialPointers =
                    ReadAliasPointerArrayPayload<MaterialAsset>(cursor, materialHandlesPointer.Untyped, numSurfs, context);
                IReadOnlyList<MaterialAsset?> materials = ReadMaterialPointers(cursor, materialPointers, context);

                var lods = new XModelLodInfo[4];
                for (int i = 0; i < 4; i++)
                {
                    int lodOffset = 0x40 + (i * XModelLodInfoSize);
                    var lodCursor = new FastFileCursor(rootBytes.AsSpan(lodOffset, XModelLodInfoSize).ToArray(), rootAddress with { Offset = rootAddress.Offset + lodOffset });
                    float dist = ReadSingle(lodCursor);
                    ushort lodNumSurfs = lodCursor.ReadUInt16();
                    ushort surfIndex = lodCursor.ReadUInt16();
                    XPointerReference modelSurfsPointer = ReadPointer<XModelSurfsAssetModel>(lodCursor, XPointerResolutionMode.AliasCell).Untyped;
                    var partBits = new uint[6];
                    for (int partBitIndex = 0; partBitIndex < partBits.Length; partBitIndex++)
                        partBits[partBitIndex] = lodCursor.ReadUInt32();
                    XPointer<byte[]> surfsRuntimePointer = ReadPointer<byte[]>(lodCursor, XPointerResolutionMode.Direct);
                    XModelSurfsAssetModel? modelSurfs = ReadXModelSurfsPointer(cursor, modelSurfsPointer, lodNumSurfs, context);
                    lods[i] = new XModelLodInfo
                    {
                        Dist = dist,
                        NumSurfs = lodNumSurfs,
                        SurfIndex = surfIndex,
                        ModelSurfsPointer = modelSurfsPointer.AsPointer<XModelSurfsAssetModel>(),
                        PartBits = partBits,
                        SurfsRuntimePointer = surfsRuntimePointer,
                        ModelSurfs = modelSurfs
                    };
                }

                IReadOnlyList<XModelCollSurf> collSurfs = ReadXModelCollSurfArray(cursor, collSurfsPointer.Untyped, numCollSurfs, context);
                IReadOnlyList<XBoneInfo> boneInfo = ReadXBoneInfoArray(cursor, boneInfoPointer.Untyped, numBones, context);
                IReadOnlyList<ushort> invHighMipRadius = ReadUInt16Array(cursor, invHighMipRadiusPointer.Untyped, numSurfs, context);
                PhysPresetAssetModel? physPreset = ReadPhysPresetPointer(cursor, physPresetPointer, context);
                PhysCollmapAssetModel? physCollmap = _physCollmapLoader.LoadFromPointer(cursor, physCollmapPointer, context);

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
                    NumSurfs = numSurfs,
                    Pad07 = pad07,
                    Scale = scale,
                    NoScalePartBits = noScalePartBits,
                    BoneNamesPointer = boneNamesPointer,
                    BoneNames = boneNames,
                    ParentListPointer = parentListPointer,
                    ParentList = parentList,
                    QuatsPointer = quatsPointer,
                    Quats = quats,
                    TransPointer = transPointer,
                    Trans = trans,
                    PartClassificationPointer = partClassificationPointer,
                    PartClassification = partClassification,
                    BaseMatPointer = baseMatPointer,
                    BaseMat = baseMat,
                    MaterialHandlesPointer = materialHandlesPointer,
                    MaterialPointers = materialPointers,
                    Materials = materials,
                    Lods = lods,
                    MaxLoadedLod = maxLoadedLod,
                    NumLods = numLods,
                    CollLod = collLod,
                    Flags = flags,
                    CollSurfsPointer = collSurfsPointer,
                    NumCollSurfs = numCollSurfs,
                    Contents = contents,
                    CollSurfs = collSurfs,
                    BoneInfoPointer = boneInfoPointer,
                    BoneInfo = boneInfo,
                    Radius = radius,
                    Bounds = bounds,
                    InvHighMipRadiusPointer = invHighMipRadiusPointer,
                    InvHighMipRadius = invHighMipRadius,
                    MemUsage = memUsage,
                    PhysPresetPointer = physPresetPointer.AsPointer<PhysPresetAssetModel>(),
                    PhysPreset = physPreset,
                    PhysCollmapPointer = physCollmapPointer.AsPointer<PhysCollmapAssetModel>(),
                    PhysCollmap = physCollmap
                };
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private XModelSurfsAssetModel? ReadXModelSurfsPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        ushort lodNumSurfs,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<XModelSurfsAssetModel>(pointer, context, XModelSurfsSize, "XModelSurfs"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<XModelSurfsAssetModel>(pointer, XModelSurfsSize, "XModelSurfs");
            return null;
        }

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        int sourceOffset = cursor.Offset;
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
            ushort pad0A = rootCursor.ReadUInt16();
            var partBits = new uint[6];
            for (int i = 0; i < partBits.Length; i++)
                partBits[i] = rootCursor.ReadUInt32();

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                string? name = ReadXString(cursor, namePointer, context);
                IReadOnlyList<XSurface> surfaces = ReadXSurfaceArray(cursor, surfsPointer.Untyped, numSurfs == 0 ? lodNumSurfs : numSurfs, context);

                if (insertCell is { } cell)
                    context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

                return new XModelSurfsAssetModel
                {
                    Offset = sourceOffset,
                    NamePointer = namePointer,
                    Name = name,
                    SurfsPointer = surfsPointer,
                    NumSurfs = numSurfs,
                    Pad0A = pad0A,
                    PartBits = partBits,
                    Surfaces = surfaces
                };
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private IReadOnlyList<XSurface> ReadXSurfaceArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0 || pointer.Type == PointerType.Null)
            return [];

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<XSurface[]>(pointer, checked(count * XSurfaceSize), "XSurface[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] surfaceBytes = context.Blocks.Load(cursor, checked(count * XSurfaceSize), out XBlockAddress arrayAddress);
        var surfaces = new XSurface[count];

        for (int i = 0; i < count; i++)
        {
            int offset = i * XSurfaceSize;
            var surfaceCursor = new FastFileCursor(surfaceBytes.AsSpan(offset, XSurfaceSize).ToArray(), arrayAddress with { Offset = arrayAddress.Offset + offset });
            surfaces[i] = ReadXSurfaceChildren(cursor, surfaceCursor, context);
        }

        return surfaces;
    }

    private XSurface ReadXSurfaceChildren(
        FastFileCursor cursor,
        FastFileCursor surfaceCursor,
        FastFileLoadContext context)
    {
        ushort flagsOrPad00 = surfaceCursor.ReadUInt16();
        byte streamFlags = surfaceCursor.ReadByte();
        byte pad03 = surfaceCursor.ReadByte();
        ushort vertCount = surfaceCursor.ReadUInt16();
        ushort triCount = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> triIndicesPointer = ReadPointer<ushort[]>(surfaceCursor, XPointerResolutionMode.Direct);
        ushort blend0 = surfaceCursor.ReadUInt16();
        ushort blend1 = surfaceCursor.ReadUInt16();
        ushort blend2 = surfaceCursor.ReadUInt16();
        ushort blend3 = surfaceCursor.ReadUInt16();
        XPointer<ushort[]> vertsBlendPointer = ReadPointer<ushort[]>(surfaceCursor, XPointerResolutionMode.Direct);
        XPointer<byte[]> verts0Pointer = ReadPointer<byte[]>(surfaceCursor, XPointerResolutionMode.Direct);
        GfxVertexBuffer vb0 = ReadGfxVertexBuffer(surfaceCursor);
        XPointer<byte[]> verts1Pointer = ReadPointer<byte[]>(surfaceCursor, XPointerResolutionMode.Direct);
        GfxVertexBuffer vb1 = ReadGfxVertexBuffer(surfaceCursor);
        int vertListCount = surfaceCursor.ReadInt32();
        XPointer<XRigidVertList[]> vertListPointer = ReadPointer<XRigidVertList[]>(surfaceCursor, XPointerResolutionMode.Direct);
        GfxIndexBuffer indexBuffer = ReadGfxIndexBuffer(surfaceCursor);
        var partBits = new uint[6];
        for (int i = 0; i < partBits.Length; i++)
            partBits[i] = surfaceCursor.ReadUInt32();

        int blendCount = blend0 + (blend1 * 3) + (blend2 * 5) + (blend3 * 7);

        IReadOnlyList<ushort> vertsBlend = ReadUInt16Array(cursor, vertsBlendPointer.Untyped, blendCount, context);
        IReadOnlyList<byte> verts0 = ReadSurfaceStreamBytes(cursor, verts0Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushPhysical: (streamFlags & 0x01) == 0, context);
        IReadOnlyList<byte> verts1 = ReadSurfaceStreamBytes(cursor, verts1Pointer.Untyped, checked(vertCount * 0x10), alignment: 16, pushPhysical: (streamFlags & 0x02) == 0, context);
        IReadOnlyList<XRigidVertList> vertList = ReadRigidVertListArray(cursor, vertListPointer.Untyped, vertListCount, context);
        IReadOnlyList<ushort> triIndices = ReadSurfaceStreamUshorts(cursor, triIndicesPointer.Untyped, checked(triCount * 3), alignment: 16, pushPhysical: (streamFlags & 0x04) == 0, context);

        return new XSurface
        {
            FlagsOrPad00 = flagsOrPad00,
            StreamFlags = streamFlags,
            Pad03 = pad03,
            VertCount = vertCount,
            TriCount = triCount,
            TriIndicesPointer = triIndicesPointer,
            TriIndices = triIndices,
            VertexInfo = new XSurfaceVertexInfo
            {
                Blend0 = blend0,
                Blend1 = blend1,
                Blend2 = blend2,
                Blend3 = blend3,
                VertsBlendPointer = vertsBlendPointer,
                VertsBlend = vertsBlend
            },
            Verts0Pointer = verts0Pointer,
            Verts0 = verts0,
            Vb0 = vb0,
            Verts1Pointer = verts1Pointer,
            Verts1 = verts1,
            Vb1 = vb1,
            VertListCount = vertListCount,
            VertListPointer = vertListPointer,
            VertList = vertList,
            IndexBuffer = indexBuffer,
            PartBits = partBits
        };
    }

    private static GfxVertexBuffer ReadGfxVertexBuffer(FastFileCursor cursor)
    {
        return new GfxVertexBuffer
        {
            StreamSource = cursor.ReadInt32(),
            DataOffset = cursor.ReadInt32()
        };
    }

    private static GfxIndexBuffer ReadGfxIndexBuffer(FastFileCursor cursor)
    {
        return new GfxIndexBuffer
        {
            DataOffset = cursor.ReadInt32()
        };
    }

    private IReadOnlyList<XRigidVertList> ReadRigidVertListArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count <= 0 || pointer.Type == PointerType.Null)
            return [];

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<XRigidVertList[]>(pointer, checked(count * XRigidVertListSize), "XRigidVertList[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] listBytes = context.Blocks.Load(cursor, checked(count * XRigidVertListSize), out XBlockAddress listAddress);
        var lists = new XRigidVertList[count];
        for (int i = 0; i < count; i++)
        {
            int offset = i * XRigidVertListSize;
            var listCursor = new FastFileCursor(listBytes.AsSpan(offset, XRigidVertListSize).ToArray(), listAddress with { Offset = listAddress.Offset + offset });
            ushort boneOffset = listCursor.ReadUInt16();
            ushort vertCount = listCursor.ReadUInt16();
            ushort triOffset = listCursor.ReadUInt16();
            ushort triCount = listCursor.ReadUInt16();
            XPointer<XSurfaceCollisionTree> collisionTreePointer = ReadPointer<XSurfaceCollisionTree>(listCursor, XPointerResolutionMode.Direct);
            lists[i] = new XRigidVertList
            {
                BoneOffset = boneOffset,
                VertCount = vertCount,
                TriOffset = triOffset,
                TriCount = triCount,
                CollisionTreePointer = collisionTreePointer,
                CollisionTree = ReadXSurfaceCollisionTree(cursor, collisionTreePointer.Untyped, context)
            };
        }

        return lists;
    }

    private XSurfaceCollisionTree? ReadXSurfaceCollisionTree(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<XSurfaceCollisionTree>(pointer, XSurfaceCollisionTreeSize, "XSurfaceCollisionTree");
            return null;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] treeBytes = context.Blocks.Load(cursor, XSurfaceCollisionTreeSize, out XBlockAddress treeAddress);
        var treeCursor = new FastFileCursor(treeBytes, treeAddress);
        ModelVec3 trans = ReadVec3(treeCursor);
        ModelVec3 scale = ReadVec3(treeCursor);
        int nodeCount = treeCursor.ReadInt32();
        XPointer<XSurfaceCollisionNode[]> nodesPointer = ReadPointer<XSurfaceCollisionNode[]>(treeCursor, XPointerResolutionMode.Direct);
        int leafCount = treeCursor.ReadInt32();
        XPointer<XSurfaceCollisionLeaf[]> leafsPointer = ReadPointer<XSurfaceCollisionLeaf[]>(treeCursor, XPointerResolutionMode.Direct);

        return new XSurfaceCollisionTree
        {
            Trans = trans,
            Scale = scale,
            NodeCount = nodeCount,
            NodesPointer = nodesPointer,
            Nodes = ReadCollisionNodeArray(cursor, nodesPointer.Untyped, nodeCount, context),
            LeafCount = leafCount,
            LeafsPointer = leafsPointer,
            Leafs = ReadCollisionLeafArray(cursor, leafsPointer.Untyped, leafCount, context)
        };
    }

    private PhysPresetAssetModel? ReadPhysPresetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<PhysPresetAssetModel>(pointer, context, PhysPresetSize, "PhysPreset"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<PhysPresetAssetModel>(pointer, PhysPresetSize, "PhysPreset");
            return null;
        }

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            int sourceOffset = cursor.Offset;
            byte[] rootBytes = context.Blocks.Load(cursor, PhysPresetSize);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor);
            rootCursor.Skip(0x1c - rootCursor.Offset);
            XString sndAliasPrefixPointer = ReadXStringPointer(rootCursor);

            string? name;
            string? sndAliasPrefix;
            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                name = ReadXString(cursor, namePointer, context);
                sndAliasPrefix = ReadXString(cursor, sndAliasPrefixPointer, context);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return new PhysPresetAssetModel
            {
                Offset = sourceOffset,
                NamePointer = namePointer,
                Name = name,
                SndAliasPrefixPointer = sndAliasPrefixPointer,
                SndAliasPrefix = sndAliasPrefix
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private IReadOnlyList<MaterialAsset?> ReadMaterialPointers(
        FastFileCursor cursor,
        IReadOnlyList<XPointer<MaterialAsset>> pointers,
        FastFileLoadContext context)
    {
        var materials = new MaterialAsset?[pointers.Count];
        for (int i = 0; i < pointers.Count; i++)
            materials[i] = ReadMaterialPointer(cursor, pointers[i].Untyped, context);

        return materials;
    }

    private MaterialAsset? ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<MaterialAsset>(pointer, context, MaterialAsset.SerializedSize, "Material"))
            return null;

        return _materialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private IReadOnlyList<byte> ReadSurfaceStreamBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        bool pushPhysical,
        FastFileLoadContext context)
    {
        if (!pushPhysical)
        {
            return ReadRawBytes(cursor, pointer, byteCount, alignment, context);
        }

        context.Blocks.Push(XFileBlockType.PHYSICAL);
        try
        {
            return ReadRawBytes(cursor, pointer, byteCount, alignment, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private IReadOnlyList<ushort> ReadSurfaceStreamUshorts(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        bool pushPhysical,
        FastFileLoadContext context)
    {
        IReadOnlyList<byte> bytes = ReadSurfaceStreamBytes(
            cursor,
            pointer,
            checked(count * sizeof(ushort)),
            alignment,
            pushPhysical,
            context);

        return ReadUInt16Values(bytes);
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
            context.PointerReader.ValidateOffsetPointerRange<XPointer<T>[]>(pointer, byteCount, $"{typeof(T).Name}*[]");
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

    private static IReadOnlyList<byte> ReadByteArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        return ReadRawBytes(cursor, pointer, count, alignment: 1, context);
    }

    private static IReadOnlyList<short> ReadInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        return ReadInt16Values(ReadRawBytes(cursor, pointer, checked(count * sizeof(short)), alignment: 2, context));
    }

    private static IReadOnlyList<ushort> ReadUInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        return ReadUInt16Values(ReadRawBytes(cursor, pointer, checked(count * sizeof(ushort)), alignment: 2, context));
    }

    private static IReadOnlyList<float> ReadFloatArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        return ReadFloatValues(ReadRawBytes(cursor, pointer, checked(count * sizeof(float)), alignment: 4, context));
    }

    private static IReadOnlyList<DObjAnimMat> ReadDObjAnimMatArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        IReadOnlyList<byte> bytes = ReadRawBytes(cursor, pointer, checked(count * DObjAnimMat.SerializedSize), alignment: 4, context);
        if (bytes.Count == 0)
            return [];

        RequireExactByteCount(bytes, count, DObjAnimMat.SerializedSize, nameof(DObjAnimMat));
        var values = new DObjAnimMat[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadDObjAnimMat(bytes, i * DObjAnimMat.SerializedSize);

        return values;
    }

    private static DObjAnimMat ReadDObjAnimMat(IReadOnlyList<byte> bytes, int offset)
    {
        var cursor = new FastFileCursor(bytes.Skip(offset).Take(DObjAnimMat.SerializedSize).ToArray());
        return new DObjAnimMat(
            new DObjQuat(ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor)),
            ReadVec3(cursor),
            ReadSingle(cursor));
    }

    private static IReadOnlyList<XModelCollSurf> ReadXModelCollSurfArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        IReadOnlyList<byte> bytes = ReadRawBytes(cursor, pointer, checked(count * XModelCollSurf.SerializedSize), alignment: 4, context);
        if (bytes.Count == 0)
            return [];

        RequireExactByteCount(bytes, count, XModelCollSurf.SerializedSize, nameof(XModelCollSurf));
        var values = new XModelCollSurf[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadXModelCollSurf(bytes, i * XModelCollSurf.SerializedSize);

        return values;
    }

    private static XModelCollSurf ReadXModelCollSurf(IReadOnlyList<byte> bytes, int offset)
    {
        var cursor = new FastFileCursor(bytes.Skip(offset).Take(XModelCollSurf.SerializedSize).ToArray());
        return new XModelCollSurf(
            ReadBounds(cursor),
            cursor.ReadInt32(),
            cursor.ReadInt32(),
            cursor.ReadInt32());
    }

    private static IReadOnlyList<XBoneInfo> ReadXBoneInfoArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        IReadOnlyList<byte> bytes = ReadRawBytes(cursor, pointer, checked(count * XBoneInfo.SerializedSize), alignment: 4, context);
        if (bytes.Count == 0)
            return [];

        RequireExactByteCount(bytes, count, XBoneInfo.SerializedSize, nameof(XBoneInfo));
        var values = new XBoneInfo[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadXBoneInfo(bytes, i * XBoneInfo.SerializedSize);

        return values;
    }

    private static XBoneInfo ReadXBoneInfo(IReadOnlyList<byte> bytes, int offset)
    {
        var cursor = new FastFileCursor(bytes.Skip(offset).Take(XBoneInfo.SerializedSize).ToArray());
        return new XBoneInfo(ReadBounds(cursor), ReadSingle(cursor));
    }

    private static IReadOnlyList<XSurfaceCollisionNode> ReadCollisionNodeArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        IReadOnlyList<byte> bytes = ReadRawBytes(cursor, pointer, checked(count * XSurfaceCollisionNode.SerializedSize), alignment: 16, context);
        if (bytes.Count == 0)
            return [];

        RequireExactByteCount(bytes, count, XSurfaceCollisionNode.SerializedSize, nameof(XSurfaceCollisionNode));
        var values = new XSurfaceCollisionNode[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadXSurfaceCollisionNode(bytes, i * XSurfaceCollisionNode.SerializedSize);

        return values;
    }

    private static XSurfaceCollisionNode ReadXSurfaceCollisionNode(IReadOnlyList<byte> bytes, int offset)
    {
        var cursor = new FastFileCursor(bytes.Skip(offset).Take(XSurfaceCollisionNode.SerializedSize).ToArray());
        var aabb = new XSurfaceCollisionAabb(
            cursor.ReadUInt16(),
            cursor.ReadUInt16(),
            cursor.ReadUInt16(),
            cursor.ReadUInt16(),
            cursor.ReadUInt16(),
            cursor.ReadUInt16());

        return new XSurfaceCollisionNode(aabb, cursor.ReadUInt16(), cursor.ReadUInt16());
    }

    private static IReadOnlyList<XSurfaceCollisionLeaf> ReadCollisionLeafArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        return ReadUInt16Array(cursor, pointer, count, context)
            .Select(value => new XSurfaceCollisionLeaf(value))
            .ToArray();
    }

    private static IReadOnlyList<byte> ReadRawBytes(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (byteCount < 0)
            throw new InvalidDataException($"Invalid negative byte count {byteCount}.");

        if (pointer.Type == PointerType.Null)
            return [];

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<byte[]>(pointer, byteCount, "byte[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        return context.Blocks.Load(cursor, byteCount);
    }

    private static void RequireExactByteCount(IReadOnlyList<byte> bytes, int count, int stride, string rowName)
    {
        int expected = checked(count * stride);
        if (bytes.Count != expected)
            throw new InvalidDataException($"{rowName} array expected 0x{expected:X} byte(s), got 0x{bytes.Count:X}.");
    }

    private static IReadOnlyList<ushort> ReadUInt16Values(IReadOnlyList<byte> bytes)
    {
        var cursor = new FastFileCursor(bytes.ToArray());
        var values = new ushort[bytes.Count / sizeof(ushort)];
        for (int i = 0; i < values.Length; i++)
            values[i] = cursor.ReadUInt16();

        return values;
    }

    private static IReadOnlyList<short> ReadInt16Values(IReadOnlyList<byte> bytes)
    {
        var values = ReadUInt16Values(bytes);
        return values.Select(value => unchecked((short)value)).ToArray();
    }

    private static IReadOnlyList<float> ReadFloatValues(IReadOnlyList<byte> bytes)
    {
        var cursor = new FastFileCursor(bytes.ToArray());
        var values = new float[bytes.Count / sizeof(float)];
        for (int i = 0; i < values.Length; i++)
            values[i] = BitConverter.Int32BitsToSingle(cursor.ReadInt32());

        return values;
    }

    private static IReadOnlyList<uint> ReadUInt32Values(FastFileCursor cursor, int count)
    {
        var values = new uint[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = cursor.ReadUInt32();

        return values;
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private static ModelBounds ReadBounds(FastFileCursor cursor)
    {
        return new ModelBounds
        {
            MidPoint = ReadVec3(cursor),
            HalfSize = ReadVec3(cursor)
        };
    }

    private static ModelVec3 ReadVec3(FastFileCursor cursor)
    {
        return new ModelVec3
        {
            X = ReadSingle(cursor),
            Y = ReadSingle(cursor),
            Z = ReadSingle(cursor)
        };
    }

    private static bool ResolveAliasCellOffset<T>(
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

            context.PointerReader.ValidateOffsetPointerRange<T>(
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
