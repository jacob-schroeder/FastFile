using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class FxReader
{
    private const int FxElemDefSize = 0xFC;
    private const int FxElemVelStateSampleSize = 96;
    private const int FxElemVisStateSampleSize = 48;
    private const int FxTrailVertexSize = 24;
    private const int FxSparkFountainDefSize = 52;

    public static FxEffectDef Read(ref ZoneReadContext context)
    {
        var asset = new FxEffectDef
        {
            Offset = context.Position,
            NamePtr = context.ReadPointer<string>(),
            Flags = context.ReadInt32(),
            TotalSize = context.ReadInt32(),
            MsecLoopingLife = context.ReadInt32(),
            ElemDefCountLooping = context.ReadInt32(),
            ElemDefCountOneShot = context.ReadInt32(),
            ElemDefCountEmission = context.ReadInt32(),
        };

        var elemDefCount = asset.ElemDefCountLooping + asset.ElemDefCountOneShot + asset.ElemDefCountEmission;
        asset.ElemDefs = context.ReadPointer<FxElemDef[]>();
        context.ResolveInlinePointer(asset.ElemDefs, (ref ZoneReadContext pointerContext, ZonePointer<FxElemDef[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) => ReadFxElemDefs(ref valueContext, elemDefCount)));
        });

        GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);

        return asset;
    }

    public static ZonePointer<FxEffectDef> ReadFxPointer(ref ZoneReadContext context)
    {
        var pointer = context.ReadPointer<FxEffectDef>();
        context.ResolveInlinePointer(pointer, ReadFxPointerValue);
        return pointer;
    }

    private static void ReadFxPointerValue(ref ZoneReadContext context, ZonePointer<FxEffectDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }
    private static FxElemDef[] ReadFxElemDefs(ref ZoneReadContext context, int count)
    {
        if (count <= 0)
            return [];

        var values = new FxElemDef[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadFxElemDef(ref context);

        foreach (var value in values)
            ResolveFxElemDefPointers(ref context, value);

        return values;
    }

    private static FxElemDef ReadFxElemDef(ref ZoneReadContext context)
    {
        var start = context.Position;
        var elem = new FxElemDef
        {
            Flags = context.ReadInt32(),
        };

        context.ReadBytes(0xA4);
        elem.ElemType = context.ReadByte();
        elem.VisualCount = context.ReadByte();
        elem.VelIntervalCount = context.ReadByte();
        elem.VisStateIntervalCount = context.ReadByte();
        elem.VelSamples = context.ReadPointer<byte[]>();
        elem.VisSamples = context.ReadPointer<byte[]>();
        elem.Visuals = context.ReadPointer<byte>();
        context.ReadBytes(24);
        elem.EffectOnImpact = context.ReadPointer<byte>();
        elem.EffectOnDeath = context.ReadPointer<byte>();
        elem.EffectEmitted = context.ReadPointer<byte>();
        context.ReadBytes(16);
        elem.Extended = context.ReadPointer<byte>();
        context.ReadBytes(4);

        var bytesRead = context.Position - start;
        if (bytesRead != FxElemDefSize)
            throw new InvalidDataException($"FxElemDef read {bytesRead:N0} bytes; expected {FxElemDefSize:N0} bytes.");

        return elem;
    }

    private static void ResolveFxElemDefPointers(ref ZoneReadContext context, FxElemDef elem)
    {
        ResolveInlineBytes(ref context, elem.VelSamples, elem.VelIntervalCount * FxElemVelStateSampleSize);
        ResolveInlineBytes(ref context, elem.VisSamples, elem.VisStateIntervalCount * FxElemVisStateSampleSize);
        ResolveVisuals(ref context, elem);
        ResolveEffectDefRefPointer(ref context, elem.EffectOnImpact);
        ResolveEffectDefRefPointer(ref context, elem.EffectOnDeath);
        ResolveEffectDefRefPointer(ref context, elem.EffectEmitted);
        ResolveExtended(ref context, elem);
    }

    private static void ResolveVisuals(ref ZoneReadContext context, FxElemDef elem)
    {
        if (elem.Visuals.Kind != PointerKind.Inline)
        {
            elem.Visuals.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(elem.Visuals, (ref ZoneReadContext pointerContext, ZonePointer<byte> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) =>
                {
                    if (elem.ElemType == 0xB)
                    {
                        for (var i = 0; i < elem.VisualCount; i++)
                        {
                            MaterialReader.ReadMaterialPointer(ref valueContext);
                            MaterialReader.ReadMaterialPointer(ref valueContext);
                        }

                        return default;
                    }

                    var visualCount = elem.VisualCount == 1 ? 1 : elem.VisualCount;
                    for (var i = 0; i < visualCount; i++)
                        ReadFxElemVisual(ref valueContext, elem.ElemType);

                    return default;
                }));
        });
    }

    private static void ReadFxElemVisual(ref ZoneReadContext context, byte elemType)
    {
        switch (elemType)
        {
            case 0x7:
                XModelReader.ReadXModelPointer(ref context);
                break;
            case 0xC:
                ReadEffectDefRefValue(ref context);
                break;
            case 0xA:
                GenericReader.ReadStringPointer(ref context);
                break;
            case 0x8:
            case 0x9:
                context.ReadPointer<byte>().SetResult(default);
                break;
            default:
                MaterialReader.ReadMaterialPointer(ref context);
                break;
        }
    }

    private static void ResolveEffectDefRefPointer(ref ZoneReadContext context, ZonePointer<byte> pointer)
    {
        if (pointer.Kind != PointerKind.Inline)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref ZoneReadContext pointerContext, ZonePointer<byte> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref ZoneReadContext valueContext) =>
                {
                    ReadEffectDefRefValue(ref valueContext);
                    return default;
                }));
        });
    }

    private static void ReadEffectDefRefValue(ref ZoneReadContext context)
    {
        var pointer = context.ReadPointer<byte>();
        if (pointer.Kind == PointerKind.Inline)
            context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<byte> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref ZoneReadContext valueContext) =>
                    {
                        GenericReader.ReadCString(ref valueContext);
                        return default;
                    }));
            });
        else
            pointer.SetResult(default);
    }

    private static void ResolveExtended(ref ZoneReadContext context, FxElemDef elem)
    {
        if (elem.Extended.Kind != PointerKind.Inline)
        {
            elem.Extended.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(elem.Extended, (ref ZoneReadContext pointerContext, ZonePointer<byte> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) =>
                {
                    switch (elem.ElemType)
                    {
                        case 0x3:
                            ReadTrailDef(ref valueContext);
                            break;
                        case 0x6:
                            valueContext.ReadBytes(FxSparkFountainDefSize);
                            break;
                        default:
                            valueContext.ReadByte();
                            break;
                    }

                    return default;
                }));
        });
    }

    private static void ReadTrailDef(ref ZoneReadContext context)
    {
        context.ReadInt32();
        context.ReadInt32();
        context.ReadFloat();
        context.ReadFloat();
        context.ReadFloat();
        var vertCount = context.ReadInt32();
        var verts = context.ReadPointer<byte[]>();
        var indCount = context.ReadInt32();
        var inds = context.ReadPointer<ushort[]>();

        ResolveInlineBytes(ref context, verts, vertCount * FxTrailVertexSize);
        ResolveInlineUShorts(ref context, inds, indCount);
    }
    private static void ResolveInlineBytes(ref ZoneReadContext context, ZonePointer<byte[]> pointer, int length)
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

    private static void ResolveInlineUShorts(ref ZoneReadContext context, ZonePointer<ushort[]> pointer, int count)
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
}
