using FastFile.Logic.Assets.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Data;
using FastFile.Models.Assets.XModels;

namespace FastFile.Logic.Assets;

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
        context.ReadByte(); // lodRampType
        context.ReadFloat(); // scale
        context.ReadBytes(NoScalePartBitsCount * 4);

        var boneNames = context.ReadPointer<ushort[]>();
        var parentList = context.ReadPointer<byte[]>();
        var quats = context.ReadPointer<byte[]>();
        var trans = context.ReadPointer<byte[]>();
        var partClassification = context.ReadPointer<byte[]>();
        var baseMat = context.ReadPointer<byte[]>();
        var materialHandles = context.ReadPointer<ZonePointer<FastFile.Models.Assets.Material.Material>[]>();

        for (var i = 0; i < LodInfoCount; i++)
            context.ReadBytes(LodInfoSize);

        context.ReadByte(); // maxLoadedLod
        var numLods = context.ReadByte();
        context.ReadBytes(2); // collLod, flags
        var collSurfs = context.ReadPointer<byte[]>();
        var numCollSurfs = context.ReadInt32();
        context.ReadInt32(); // contents
        var boneInfo = context.ReadPointer<byte[]>();
        context.ReadFloat(); // radius
        context.ReadBytes(BoundsSize);
        context.ReadInt32(); // memUsage
        context.ReadByte(); // bad
        context.ReadBytes(3);
        PhysicsReader.ReadPhysPresetPointer(ref context);
        PhysicsReader.ReadPhysCollmapPointer(ref context);

        GenericReader.ResolveStringPointerNow(ref context, name);
        ResolveInlineUShortArray(ref context, boneNames, numBones);
        ResolveInlineArray(ref context, parentList, Math.Max(0, numBones - numRootBones));
        ResolveInlineArray(ref context, quats, Math.Max(0, numBones - numRootBones) * 8);
        ResolveInlineArray(ref context, trans, Math.Max(0, numBones - numRootBones) * 12);
        ResolveInlineArray(ref context, partClassification, numBones);
        ResolveInlineArray(ref context, baseMat, numBones * 32);
        ResolveMaterialHandleArray(ref context, materialHandles, numSurfs);
        ResolveInlineArray(ref context, collSurfs, numCollSurfs * 44);
        ResolveInlineArray(ref context, boneInfo, numBones * 28);

        return new XModel
        {
            Offset = offset,
            NamePtr = name,
            NumBones = numBones,
            NumRootBones = numRootBones,
            NumSurfs = numSurfs,
            NumLods = numLods,
        };
    }
    private static void ResolveInlineArray(
        ref ZoneReadContext context,
        ZonePointer<byte[]> pointer,
        int length)
    {
        if (length <= 0 || pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<byte[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) => valueContext.ReadBytes(length)));
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
            });
    }
}
