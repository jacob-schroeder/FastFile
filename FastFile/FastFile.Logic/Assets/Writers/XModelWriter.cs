using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;

namespace FastFile.Logic.Assets.Writers;

internal static class XModelWriter
{
    private const int LodInfoCount = 4;

    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteXModelValue(context, (XModel)asset);
    }

    public static void WriteXModelPointer(ZoneWriterContext context, ZonePointer<XModel>? pointer)
    {
        context.WritePointer(pointer, WriteXModelPointerValue);
    }

    public static void WriteXModelPointerArrayPointer(
        ZoneWriterContext context,
        ZonePointer<ZonePointer<XModel>[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var model in p.Result ?? [])
                WriteXModelPointer(pointerContext, model);
        });
    }

    private static void WriteXModelPointerValue(ZoneWriterContext context, ZonePointer<XModel> pointer)
    {
        if (pointer.Result is { } value)
            WriteXModelValue(context, value);
    }

    private static void WriteXModelValue(ZoneWriterContext context, XModel asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WriteByte(asset.NumBones);
        context.WriteByte(asset.NumRootBones);
        context.WriteByte(asset.NumSurfs);
        context.WriteByte(asset.LodRampType);
        context.WriteFloat(asset.Scale);
        WriteInt32Array(context, asset.NoScalePartBits, 6);
        WriteUShortArrayPointer(context, asset.BoneNames);
        WriteParentArrayPointer(context, asset.ParentList);
        WriteQuatArrayPointer(context, asset.Quats);
        WriteVec3ArrayPointer(context, asset.Trans);
        WritePartClassificationArrayPointer(context, asset.PartClassification);
        WriteDObjAnimMatArrayPointer(context, asset.BaseMat);
        WriteMaterialHandleArrayPointer(context, asset.MaterialHandles);

        for (var i = 0; i < LodInfoCount; i++)
            WriteXModelLodInfo(context, GetFixedValue(asset.LodInfo, i, nameof(asset.LodInfo)));

        context.WriteByte(asset.MaxLoadedLod);
        context.WriteByte(asset.NumLods);
        context.WriteByte(asset.CollLod);
        context.WriteByte(asset.Flags);
        WriteXModelCollSurfArrayPointer(context, asset.CollSurfs);
        context.WriteInt32(asset.NumCollSurfs);
        context.WriteInt32(asset.Contents);
        WriteXBoneInfoArrayPointer(context, asset.BoneInfo);
        context.WriteFloat(asset.Radius);
        context.WriteBounds(asset.Bounds);
        context.WriteInt32(asset.MemUsage);
        context.WriteBool(asset.Bad);
        context.WriteByte(asset.BadPadding0);
        context.WriteByte(asset.BadPadding1);
        context.WriteByte(asset.BadPadding2);
        PhysicsWriter.WritePhysPresetPointer(context, asset.PhysPreset);
        PhysicsWriter.WritePhysCollmapPointer(context, asset.PhysCollmap);
    }

    private static void WriteXModelLodInfo(ZoneWriterContext context, XModelLodInfo lodInfo)
    {
        context.WriteFloat(lodInfo.Dist);
        context.WriteUInt16(lodInfo.NumSurfs);
        context.WriteUInt16(lodInfo.SurfIndex);
        XModelSurfsWriter.WriteXModelSurfsPointer(context, lodInfo.ModelSurfs);
        WriteInt32Array(context, lodInfo.PartBits, 6);
        context.WritePointerRaw(lodInfo.Surfs);
    }

    private static void WriteUShortArrayPointer(ZoneWriterContext context, ZonePointer<ushort[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteUInt16(value);
        });
    }

    private static void WriteParentArrayPointer(ZoneWriterContext context, ZonePointer<XModelParent[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteByte(value.BoneIndex);
        });
    }

    private static void WriteQuatArrayPointer(ZoneWriterContext context, ZonePointer<XModelQuat[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
            {
                pointerContext.WriteInt16(value.X);
                pointerContext.WriteInt16(value.Y);
                pointerContext.WriteInt16(value.Z);
                pointerContext.WriteInt16(value.W);
            }
        });
    }

    private static void WriteVec3ArrayPointer(ZoneWriterContext context, ZonePointer<Vec3[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteVec3(value);
        });
    }

    private static void WritePartClassificationArrayPointer(
        ZoneWriterContext context,
        ZonePointer<XModelPartClassification[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                pointerContext.WriteByte(value.Value);
        });
    }

    private static void WriteDObjAnimMatArrayPointer(ZoneWriterContext context, ZonePointer<DObjAnimMat[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
            {
                pointerContext.WriteVec4(value.Quat);
                pointerContext.WriteVec3(value.Trans);
                pointerContext.WriteFloat(value.TransWeight);
            }
        });
    }

    private static void WriteMaterialHandleArrayPointer(
        ZoneWriterContext context,
        ZonePointer<ZonePointer<Material>[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                MaterialWriter.WriteMaterialPointer(pointerContext, value);
        });
    }

    private static void WriteXModelCollSurfArrayPointer(
        ZoneWriterContext context,
        ZonePointer<XModelCollSurf[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                WriteXModelCollSurf(pointerContext, value);
        });
    }

    private static void WriteXModelCollSurf(ZoneWriterContext context, XModelCollSurf value)
    {
        context.WritePointerRaw(value.CollTris);
        context.WriteInt32(value.NumCollTris);
        context.WriteBounds(value.Bounds);
        context.WriteInt32(value.BoneIdx);
        context.WriteInt32(value.Contents);
        context.WriteInt32(value.SurfFlags);
    }

    private static void WriteXBoneInfoArrayPointer(ZoneWriterContext context, ZonePointer<XBoneInfo[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
            {
                pointerContext.WriteBounds(value.Bounds);
                pointerContext.WriteFloat(value.RadiusSquared);
            }
        });
    }

    private static void WriteInt32Array(ZoneWriterContext context, int[]? values, int count)
    {
        if ((values?.Length ?? 0) < count)
            throw new InvalidDataException($"Expected {count:N0} values, but found {values?.Length ?? 0:N0}.");

        for (var i = 0; i < count; i++)
            context.WriteInt32(values![i]);
    }

    private static T GetFixedValue<T>(T[]? values, int index, string fieldName)
    {
        if (values is null || values.Length <= index || values[index] is null)
            throw new InvalidDataException($"{fieldName} is missing element {index:N0}.");

        return values[index];
    }
}
