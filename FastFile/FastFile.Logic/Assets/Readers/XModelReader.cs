using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Utils;

namespace FastFile.Logic.Assets.Readers;

internal static class XModelReader
{
    private const int NoScalePartBitsCount = 6;
    private const int LodInfoCount = 4;
    private const int LodInfoSize = 40;
    private const int BoundsSize = 24;

    public static ZonePointer<XModel> ReadXModelPointer(ref ZoneReadContext context)
    {
        var pointer = context.ReadPointer<XModel>();
        context.ResolveInlinePointer(pointer, ReadXModelPointerValue);
        return pointer;
    }

    public static ZonePointer<ZonePointer<XModel>[]> ReadXModelPointerArrayPointer(
        ref ZoneReadContext context,
        int count)
    {
        var pointer = context.ReadPointer<ZonePointer<XModel>[]>();
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<XModel>[]> p) =>
        {
            var values = new ZonePointer<XModel>[count];
            for (var i = 0; i < values.Length; i++)
                values[i] = pointerContext.ReadPointer<XModel>();

            p.SetResult(values);

            foreach (var value in values)
            {
                if (value.Kind == PointerKind.Inline)
                    pointerContext.ResolveInlinePointerDeferred(value, ReadXModelPointerValue);
                else
                    value.SetResult(default);
            }
        });

        return pointer;
    }

    private static void ReadXModelPointerValue(ref ZoneReadContext context, ZonePointer<XModel> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }

    public static XModel Read(ref ZoneReadContext context)
    {
        var offset = context.Position;
        var name = GenericReader.ReadStringPointer(ref context, resolve: false);
        var numBones = context.ReadByte();
        var numRootBones = context.ReadByte();
        var numSurfs = context.ReadByte();
        var lodRampType = context.ReadByte();
        var scale = context.ReadFloat();
        var noScalePartBits = ReadInt32Array(ref context, NoScalePartBitsCount);

        var boneNames = context.ReadPointer<ushort[]>();
        var parentList = context.ReadPointer<XModelParent[]>();
        var quats = context.ReadPointer<XModelQuat[]>();
        var trans = context.ReadPointer<Vec3[]>();
        var partClassification = context.ReadPointer<XModelPartClassification[]>();
        var baseMat = context.ReadPointer<DObjAnimMat[]>();
        var materialHandles = context.ReadPointer<ZonePointer<FastFile.Models.Assets.Material.Material>[]>();

        var lodInfo = new XModelLodInfo[LodInfoCount];
        for (var i = 0; i < lodInfo.Length; i++)
            lodInfo[i] = ReadXModelLodInfo(ref context);

        var maxLoadedLod = context.ReadByte();
        var numLods = context.ReadByte();
        var collLod = context.ReadByte();
        var flags = context.ReadByte();
        var collSurfs = context.ReadPointer<XModelCollSurf[]>();
        var numCollSurfs = context.ReadInt32();
        var contents = context.ReadInt32();
        var boneInfo = context.ReadPointer<XBoneInfo[]>();
        var radius = context.ReadFloat();
        var bounds = ReadBounds(ref context);
        var memUsage = context.ReadInt32();
        var bad = context.ReadBool();
        var badPadding0 = context.ReadByte();
        var badPadding1 = context.ReadByte();
        var badPadding2 = context.ReadByte();
        var physPreset = PhysicsReader.ReadPhysPresetPointer(ref context);
        var physCollmap = PhysicsReader.ReadPhysCollmapPointer(ref context);

        GenericReader.ResolveStringPointerNow(ref context, name);
        ResolveInlineUShortArray(ref context, boneNames, numBones);
        ResolveParentArray(ref context, parentList, Math.Max(0, numBones - numRootBones));
        ResolveQuatArray(ref context, quats, Math.Max(0, numBones - numRootBones));
        ResolveVec3Array(ref context, trans, Math.Max(0, numBones - numRootBones));
        ResolvePartClassificationArray(ref context, partClassification, numBones);
        ResolveDObjAnimMatArray(ref context, baseMat, numBones);
        ResolveMaterialHandleArray(ref context, materialHandles, numSurfs);
        ResolveXModelCollSurfArray(ref context, collSurfs, numCollSurfs);
        ResolveXBoneInfoArray(ref context, boneInfo, numBones);

        return new XModel
        {
            Offset = offset,
            NamePtr = name,
            NumBones = numBones,
            NumRootBones = numRootBones,
            NumSurfs = numSurfs,
            LodRampType = lodRampType,
            Scale = scale,
            NoScalePartBits = noScalePartBits,
            BoneNames = boneNames,
            ParentList = parentList,
            Quats = quats,
            Trans = trans,
            PartClassification = partClassification,
            BaseMat = baseMat,
            MaterialHandles = materialHandles,
            LodInfo = lodInfo,
            MaxLoadedLod = maxLoadedLod,
            NumLods = numLods,
            CollLod = collLod,
            Flags = flags,
            CollSurfs = collSurfs,
            NumCollSurfs = numCollSurfs,
            Contents = contents,
            BoneInfo = boneInfo,
            Radius = radius,
            Bounds = bounds,
            MemUsage = memUsage,
            Bad = bad,
            BadPadding0 = badPadding0,
            BadPadding1 = badPadding1,
            BadPadding2 = badPadding2,
            PhysPreset = physPreset,
            PhysCollmap = physCollmap,
        };
    }

    private static XModelLodInfo ReadXModelLodInfo(ref ZoneReadContext context)
    {
        var start = context.Position;
        var lodInfo = new XModelLodInfo
        {
            Dist = context.ReadFloat(),
            NumSurfs = context.ReadUInt16(),
            SurfIndex = context.ReadUInt16(),
            ModelSurfs = context.ReadPointer<XModelSurfs>(),
        };

        for (var i = 0; i < lodInfo.PartBits.Length; i++)
            lodInfo.PartBits[i] = context.ReadInt32();

        lodInfo.Surfs = context.ReadPointer<XSurface[]>();

        var bytesRead = context.Position - start;
        if (bytesRead != LodInfoSize)
            throw new InvalidDataException($"XModelLodInfo read {bytesRead:N0} bytes; expected {LodInfoSize:N0} bytes.");

        return lodInfo;
    }

    private static void ResolveParentArray(
        ref ZoneReadContext context,
        ZonePointer<XModelParent[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<XModelParent[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new XModelParent[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = new XModelParent(valueContext.ReadByte());

                    return values;
                }));
        });
    }

    private static void ResolveQuatArray(
        ref ZoneReadContext context,
        ZonePointer<XModelQuat[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<XModelQuat[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new XModelQuat[count];
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = new XModelQuat(
                            unchecked((short)valueContext.ReadUInt16()),
                            unchecked((short)valueContext.ReadUInt16()),
                            unchecked((short)valueContext.ReadUInt16()),
                            unchecked((short)valueContext.ReadUInt16()));
                    }

                    return values;
                }));
        });
    }

    private static void ResolveVec3Array(
        ref ZoneReadContext context,
        ZonePointer<Vec3[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<Vec3[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new Vec3[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = ReadVec3(ref valueContext);

                    return values;
                }));
        });
    }

    private static void ResolvePartClassificationArray(
        ref ZoneReadContext context,
        ZonePointer<XModelPartClassification[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<XModelPartClassification[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new XModelPartClassification[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = new XModelPartClassification(valueContext.ReadByte());

                    return values;
                }));
        });
    }

    private static void ResolveDObjAnimMatArray(
        ref ZoneReadContext context,
        ZonePointer<DObjAnimMat[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<DObjAnimMat[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new DObjAnimMat[count];
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = new DObjAnimMat
                        {
                            Quat = valueContext.ReadVec4(),
                            Trans = ReadVec3(ref valueContext),
                            TransWeight = valueContext.ReadFloat(),
                        };
                    }

                    return values;
                }));
        });
    }

    private static void ResolveXModelCollSurfArray(
        ref ZoneReadContext context,
        ZonePointer<XModelCollSurf[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<XModelCollSurf[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new XModelCollSurf[count];
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = new XModelCollSurf
                        {
                            CollTris = valueContext.ReadPointer<XModelCollTri[]>(),
                            NumCollTris = valueContext.ReadInt32(),
                            Bounds = ReadBounds(ref valueContext),
                            BoneIdx = valueContext.ReadInt32(),
                            Contents = valueContext.ReadInt32(),
                            SurfFlags = valueContext.ReadInt32(),
                        };
                    }

                    return values;
                }));
        });
    }

    private static void ResolveXBoneInfoArray(
        ref ZoneReadContext context,
        ZonePointer<XBoneInfo[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<XBoneInfo[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new XBoneInfo[count];
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = new XBoneInfo
                        {
                            Bounds = ReadBounds(ref valueContext),
                            RadiusSquared = valueContext.ReadFloat(),
                        };
                    }

                    return values;
                }));
        });
    }

    private static void ResolveInlineUShortArray(
        ref ZoneReadContext context,
        ZonePointer<ushort[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<ushort[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    var values = new ushort[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = valueContext.ReadUInt16();

                    return values;
                }));
        });
    }

    private static void ResolveMaterialHandleArray(
        ref ZoneReadContext context,
        ZonePointer<ZonePointer<FastFile.Models.Assets.Material.Material>[]> pointer,
        int count)
    {
        if (count <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref ZoneReadContext pointerContext, ZonePointer<ZonePointer<FastFile.Models.Assets.Material.Material>[]> p) =>
            {
                var values = new ZonePointer<FastFile.Models.Assets.Material.Material>[count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadPointer<FastFile.Models.Assets.Material.Material>();

                p.SetResult(values);

                foreach (var value in values)
                {
                    pointerContext.ResolveInlinePointer(
                        value,
                        (ref ZoneReadContext materialContext, ZonePointer<FastFile.Models.Assets.Material.Material> materialPointer) =>
                        {
                            materialPointer.SetResult(materialContext.ReadPointerValue(materialPointer, MaterialReader.Read));
                        });
                }
            });
    }

    private static int[] ReadInt32Array(ref ZoneReadContext context, int count)
    {
        var values = new int[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadInt32();

        return values;
    }

    private static Bounds ReadBounds(ref ZoneReadContext context)
    {
        return new Bounds
        {
            MidPoint = ReadVec3(ref context),
            HalfSize = ReadVec3(ref context),
        };
    }

    private static Vec3 ReadVec3(ref ZoneReadContext context)
    {
        return new Vec3
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            Z = context.ReadFloat(),
        };
    }
}
