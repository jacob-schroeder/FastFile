using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class FxWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteFxEffectDef(context, (FxEffectDef)asset);
    }

    public static void WriteFxPointer(ZoneWriterContext context, ZonePointer<FxEffectDef>? pointer)
    {
        context.WritePointer(pointer, WriteFxPointerValue);
    }

    private static void WriteFxPointerValue(ZoneWriterContext context, ZonePointer<FxEffectDef> pointer)
    {
        if (pointer.Result is { } value)
            WriteFxEffectDef(context, value);
    }

    private static void WriteFxEffectDef(ZoneWriterContext context, FxEffectDef asset)
    {
        GenericWriter.WriteStringPointer(context, asset.NamePtr);
        context.WriteInt32(asset.Flags);
        context.WriteInt32(asset.TotalSize);
        context.WriteInt32(asset.MsecLoopingLife);
        context.WriteInt32(asset.ElemDefCountLooping);
        context.WriteInt32(asset.ElemDefCountOneShot);
        context.WriteInt32(asset.ElemDefCountEmission);
        context.WritePointer(asset.ElemDefs, WriteFxElemDefArrayPointerValue);
    }

    private static void WriteFxElemDefArrayPointerValue(ZoneWriterContext context, ZonePointer<FxElemDef[]> pointer)
    {
        foreach (var elem in pointer.Result ?? [])
            WriteFxElemDef(context, elem);
    }

    private static void WriteFxElemDef(ZoneWriterContext context, FxElemDef elem)
    {
        context.WriteInt32(elem.Flags);
        WriteFxSpawnDef(context, elem.Spawn);
        WriteFxFloatRange(context, elem.SpawnRange);
        WriteFxFloatRange(context, elem.FadeInRange);
        WriteFxFloatRange(context, elem.FadeOutRange);
        context.WriteFloat(elem.SpawnFrustumCullRadius);
        WriteFxIntRange(context, elem.SpawnDelayMsec);
        WriteFxIntRange(context, elem.LifeSpanMsec);
        foreach (var value in elem.SpawnOrigin)
            WriteFxFloatRange(context, value);
        WriteFxFloatRange(context, elem.SpawnOffsetRadius);
        WriteFxFloatRange(context, elem.SpawnOffsetHeight);
        foreach (var value in elem.SpawnAngles)
            WriteFxFloatRange(context, value);
        foreach (var value in elem.AngularVelocity)
            WriteFxFloatRange(context, value);
        WriteFxFloatRange(context, elem.InitialRotation);
        WriteFxFloatRange(context, elem.Gravity);
        WriteFxFloatRange(context, elem.ReflectionFactor);
        WriteFxElemAtlas(context, elem.Atlas);
        context.WriteByte(elem.ElemType);
        context.WriteByte(elem.VisualCount);
        context.WriteByte(elem.VelIntervalCount);
        context.WriteByte(elem.VisStateIntervalCount);
        WriteVelSamplesPointer(context, elem.VelSamples);
        WriteVisSamplesPointer(context, elem.VisSamples);
        WriteVisualsPointer(context, elem);
        context.WriteBounds(elem.CollBounds);
        WriteEffectDefRefPointer(context, elem.EffectOnImpact);
        WriteEffectDefRefPointer(context, elem.EffectOnDeath);
        WriteEffectDefRefPointer(context, elem.EffectEmitted);
        WriteFxFloatRange(context, elem.EmitDist);
        WriteFxFloatRange(context, elem.EmitDistVariance);
        WriteExtendedPointer(context, elem);
        context.WriteByte(elem.SortOrder);
        context.WriteByte(elem.LightingFrac);
        context.WriteByte(elem.UseItemClip);
        context.WriteByte(elem.FadeInfo);
    }

    private static void WriteVelSamplesPointer(ZoneWriterContext context, ZonePointer<FxElemVelStateSample[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                WriteVelStateSample(pointerContext, value);
        });
    }

    private static void WriteVisSamplesPointer(ZoneWriterContext context, ZonePointer<FxElemVisStateSample[]>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            foreach (var value in p.Result ?? [])
                WriteVisStateSample(pointerContext, value);
        });
    }

    private static void WriteVisualsPointer(ZoneWriterContext context, FxElemDef elem)
    {
        context.WritePointer(elem.Visuals, (pointerContext, p) =>
        {
            foreach (var visual in p.Result ?? [])
                WriteFxElemVisual(pointerContext, elem.ElemType, visual);
        });
    }

    private static void WriteFxElemVisual(ZoneWriterContext context, byte elemType, FxElemVisual visual)
    {
        switch (elemType)
        {
            case 0x7:
                XModelWriter.WriteXModelPointer(context, visual.Model);
                break;
            case 0xC:
                WriteEffectDefRef(context, visual.EffectDef);
                break;
            case 0xA:
                GenericWriter.WriteStringPointer(context, visual.SoundName);
                break;
            case 0x8:
            case 0x9:
                context.WritePointerRaw(visual.Anonymous);
                break;
            case 0xB:
                MaterialWriter.WriteMaterialPointer(context, visual.DecalMaterial0);
                MaterialWriter.WriteMaterialPointer(context, visual.DecalMaterial1);
                break;
            default:
                MaterialWriter.WriteMaterialPointer(context, visual.Material);
                break;
        }
    }

    private static void WriteEffectDefRefPointer(ZoneWriterContext context, ZonePointer<FxEffectDefRef>? pointer)
    {
        context.WritePointer(pointer, (pointerContext, p) =>
        {
            if (p.Result is { } value)
                WriteEffectDefRef(pointerContext, value);
        });
    }

    private static void WriteEffectDefRef(ZoneWriterContext context, FxEffectDefRef? reference)
    {
        if (reference is null)
        {
            context.WriteInt32(0);
            return;
        }

        if (reference.Name is { Kind: PointerKind.Inline, Result: not null })
        {
            GenericWriter.WriteStringPointer(context, reference.Name);
            return;
        }

        if (reference.Handle is { Kind: PointerKind.Inline, Result: not null })
        {
            WriteFxPointer(context, reference.Handle);
            return;
        }

        if (reference.Handle is not null)
        {
            context.WritePointerRaw(reference.Handle);
            return;
        }

        context.WritePointerRaw(reference.Name);
    }

    private static void WriteExtendedPointer(ZoneWriterContext context, FxElemDef elem)
    {
        context.WritePointer(elem.Extended, (pointerContext, p) =>
        {
            if (p.Result is { } value)
                WriteExtended(pointerContext, elem.ElemType, value);
        });
    }

    private static void WriteExtended(ZoneWriterContext context, byte elemType, FxElemExtendedDef value)
    {
        switch (elemType)
        {
            case 0x3:
                WriteTrailDef(context, value.TrailDef);
                break;
            case 0x6:
                WriteSparkFountainDef(context, value.SparkFountainDef);
                break;
            default:
                context.WriteByte(value.UnknownDef);
                break;
        }
    }

    private static void WriteTrailDef(ZoneWriterContext context, FxTrailDef value)
    {
        context.WriteInt32(value.ScrollTimeMsec);
        context.WriteInt32(value.RepeatDist);
        context.WriteFloat(value.InvSplitDist);
        context.WriteFloat(value.InvSplitArcDist);
        context.WriteFloat(value.InvSplitTime);
        context.WriteInt32(value.VertCount);
        context.WritePointer(value.Verts, WriteTrailVertexArrayPointerValue);
        context.WriteInt32(value.IndCount);
        context.WritePointer(value.Inds, WriteUShortArrayPointerValue);
    }

    private static void WriteTrailVertexArrayPointerValue(ZoneWriterContext context, ZonePointer<FxTrailVertex[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
        {
            context.WriteFloat(value.Pos0);
            context.WriteFloat(value.Pos1);
            context.WriteFloat(value.Normal0);
            context.WriteFloat(value.Normal1);
            context.WriteFloat(value.TexCoord);
            context.WriteInt32(value.AlignmentPadding);
        }
    }

    private static void WriteUShortArrayPointerValue(ZoneWriterContext context, ZonePointer<ushort[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            context.WriteUInt16(value);
    }

    private static void WriteSparkFountainDef(ZoneWriterContext context, FxSparkFountainDef value)
    {
        context.WriteFloat(value.Gravity);
        context.WriteFloat(value.BounceFrac);
        context.WriteFloat(value.BounceRand);
        context.WriteFloat(value.SparkSpacing);
        context.WriteFloat(value.SparkLength);
        context.WriteInt32(value.SparkCount);
        context.WriteFloat(value.LoopTime);
        context.WriteFloat(value.VelMin);
        context.WriteFloat(value.VelMax);
        context.WriteFloat(value.VelConeFrac);
        context.WriteFloat(value.RestSpeed);
        context.WriteFloat(value.BoostTime);
        context.WriteFloat(value.BoostFactor);
    }

    private static void WriteFxSpawnDef(ZoneWriterContext context, FxSpawnDef value)
    {
        context.WriteInt32(value.LoopingIntervalMsec);
        context.WriteInt32(value.Count);
    }

    private static void WriteFxIntRange(ZoneWriterContext context, FxIntRange value)
    {
        context.WriteInt32(value.Base);
        context.WriteInt32(value.Amplitude);
    }

    private static void WriteFxFloatRange(ZoneWriterContext context, FxFloatRange value)
    {
        context.WriteFloat(value.Base);
        context.WriteFloat(value.Amplitude);
    }

    private static void WriteFxElemAtlas(ZoneWriterContext context, FxElemAtlas value)
    {
        context.WriteByte(value.Behavior);
        context.WriteByte(value.Index);
        context.WriteByte(value.Fps);
        context.WriteByte(value.LoopCount);
        context.WriteByte(value.ColIndexBits);
        context.WriteByte(value.RowIndexBits);
        context.WriteInt16(value.EntryCount);
    }

    private static void WriteVelStateSample(ZoneWriterContext context, FxElemVelStateSample value)
    {
        WriteVelStateInFrame(context, value.Local);
        WriteVelStateInFrame(context, value.World);
    }

    private static void WriteVelStateInFrame(ZoneWriterContext context, FxElemVelStateInFrame value)
    {
        WriteVec3Range(context, value.Velocity);
        WriteVec3Range(context, value.TotalDelta);
    }

    private static void WriteVec3Range(ZoneWriterContext context, FxElemVec3Range value)
    {
        context.WriteVec3(value.Base);
        context.WriteVec3(value.Amplitude);
    }

    private static void WriteVisStateSample(ZoneWriterContext context, FxElemVisStateSample value)
    {
        WriteVisualState(context, value.Base);
        WriteVisualState(context, value.Amplitude);
    }

    private static void WriteVisualState(ZoneWriterContext context, FxElemVisualState value)
    {
        context.WriteByte(value.Color.R);
        context.WriteByte(value.Color.G);
        context.WriteByte(value.Color.B);
        context.WriteByte(value.Color.A);
        context.WriteFloat(value.RotationDelta);
        context.WriteFloat(value.RotationTotal);
        context.WriteFloat(value.Size0);
        context.WriteFloat(value.Size1);
        context.WriteFloat(value.Scale);
    }
}
