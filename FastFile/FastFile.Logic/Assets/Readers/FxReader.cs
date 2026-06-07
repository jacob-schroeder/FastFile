using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class FxReader
{
    private const int FxElemDefSize = 0xFC;
    private const int FxElemVelStateSampleSize = 96;
    private const int FxElemVisStateSampleSize = 48;
    private const int FxTrailVertexSize = 20;
    private const int FxSparkFountainDefSize = 52;
    private static readonly bool TraceFx = IsTraceEnabled("FASTFILE_TRACE_FX");
    private static readonly int TraceFxStart = GetTraceInt("FASTFILE_TRACE_FX_START", 0);
    private static readonly int TraceFxEnd = GetTraceInt("FASTFILE_TRACE_FX_END", int.MaxValue);
    private static readonly int TraceFxLimit = GetTraceInt("FASTFILE_TRACE_FX_LIMIT", 4096);
    private static int _traceFxCount;

    public static FxEffectDef Read(ref XFileReadContext context)
    {
        var asset = new FxEffectDef
        {
            Offset = context.Position,
            NamePtr = context.ReadDirectPointer<string>("FxEffectDef.Name"),
            Flags = context.ReadInt32(),
            TotalSize = context.ReadInt32(),
            MsecLoopingLife = context.ReadInt32(),
            ElemDefCountLooping = context.ReadInt32(),
            ElemDefCountOneShot = context.ReadInt32(),
            ElemDefCountEmission = context.ReadInt32(),
        };

        var elemDefCount = asset.ElemDefCountLooping + asset.ElemDefCountOneShot + asset.ElemDefCountEmission;
        asset.ElemDefs = context.ReadDirectPointer<FxElemDef[]>("FxEffectDef.ElemDefs");
        Trace(
            ref context,
            $"root src=0x{asset.Offset:X8} end=0x{context.Position:X8} nameRaw=0x{asset.NamePtr.Raw:X8} "
            + $"flags=0x{asset.Flags:X8} total=0x{asset.TotalSize:X} counts={asset.ElemDefCountLooping}+{asset.ElemDefCountOneShot}+{asset.ElemDefCountEmission} "
            + $"elemDefsRaw=0x{asset.ElemDefs.Raw:X8}");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            context.ResolveInlinePointer(asset.ElemDefs, (ref XFileReadContext pointerContext, ZonePointer<FxElemDef[]> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) => ReadFxElemDefs(ref valueContext, elemDefCount)));
            });

            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    public static ZonePointer<FxEffectDef> ReadFxPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<FxEffectDef>("FxEffectAssetRef");
        context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadFxPointerValue);
        return pointer;
    }

    private static void ReadFxPointerValue(ref XFileReadContext context, ZonePointer<FxEffectDef> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }
    private static FxElemDef[] ReadFxElemDefs(ref XFileReadContext context, int count)
    {
        var start = context.Position;
        Trace(ref context, $"elemDefs begin src=0x{start:X8} count={count}");

        if (count <= 0)
            return [];

        var values = new FxElemDef[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadFxElemDef(ref context, i);

        foreach (var value in values)
            ResolveFxElemDefPointers(ref context, value);

        Trace(ref context, $"elemDefs end src=0x{start:X8} end=0x{context.Position:X8} len=0x{context.Position - start:X}");
        return values;
    }

    private static FxElemDef ReadFxElemDef(ref XFileReadContext context, int index)
    {
        var start = context.Position;
        var elem = new FxElemDef
        {
            Flags = context.ReadInt32(),
            Spawn = ReadFxSpawnDef(ref context),
            SpawnRange = ReadFxFloatRange(ref context),
            FadeInRange = ReadFxFloatRange(ref context),
            FadeOutRange = ReadFxFloatRange(ref context),
            SpawnFrustumCullRadius = context.ReadFloat(),
            SpawnDelayMsec = ReadFxIntRange(ref context),
            LifeSpanMsec = ReadFxIntRange(ref context),
        };

        for (var i = 0; i < elem.SpawnOrigin.Length; i++)
            elem.SpawnOrigin[i] = ReadFxFloatRange(ref context);

        elem.SpawnOffsetRadius = ReadFxFloatRange(ref context);
        elem.SpawnOffsetHeight = ReadFxFloatRange(ref context);

        for (var i = 0; i < elem.SpawnAngles.Length; i++)
            elem.SpawnAngles[i] = ReadFxFloatRange(ref context);
        for (var i = 0; i < elem.AngularVelocity.Length; i++)
            elem.AngularVelocity[i] = ReadFxFloatRange(ref context);

        elem.InitialRotation = ReadFxFloatRange(ref context);
        elem.Gravity = ReadFxFloatRange(ref context);
        elem.ReflectionFactor = ReadFxFloatRange(ref context);
        elem.Atlas = ReadFxElemAtlas(ref context);
        elem.ElemType = context.ReadByte();
        elem.VisualCount = context.ReadByte();
        elem.VelIntervalCount = context.ReadByte();
        elem.VisStateIntervalCount = context.ReadByte();
        elem.VelSamples = context.ReadDirectPointer<FxElemVelStateSample[]>("FxElemDef.VelSamples");
        elem.VisSamples = context.ReadDirectPointer<FxElemVisStateSample[]>("FxElemDef.VisSamples");
        elem.Visuals = ReadFxElemVisualsField(ref context, elem);
        elem.CollBounds = ReadBounds(ref context);
        elem.EffectOnImpact = context.ReadDirectPointer<FxEffectDefRef>("FxElemDef.EffectOnImpact");
        elem.EffectOnDeath = context.ReadDirectPointer<FxEffectDefRef>("FxElemDef.EffectOnDeath");
        elem.EffectEmitted = context.ReadDirectPointer<FxEffectDefRef>("FxElemDef.EffectEmitted");
        elem.EmitDist = ReadFxFloatRange(ref context);
        elem.EmitDistVariance = ReadFxFloatRange(ref context);
        elem.Extended = context.ReadDirectPointer<FxElemExtendedDef>("FxElemDef.Extended");
        elem.SortOrder = context.ReadByte();
        elem.LightingFrac = context.ReadByte();
        elem.UseItemClip = context.ReadByte();
        elem.FadeInfo = context.ReadByte();

        var bytesRead = context.Position - start;
        if (bytesRead != FxElemDefSize)
            throw new InvalidDataException($"FxElemDef read {bytesRead:N0} bytes; expected {FxElemDefSize:N0} bytes.");

        Trace(
            ref context,
            $"elem[{index:D2}] root src=0x{start:X8} end=0x{context.Position:X8} "
            + $"type=0x{elem.ElemType:X2} visualCount={elem.VisualCount} velIntervals={elem.VelIntervalCount} visIntervals={elem.VisStateIntervalCount} "
            + $"velRaw=0x{elem.VelSamples.Raw:X8} visRaw=0x{elem.VisSamples.Raw:X8} visualRaw=0x{elem.Visuals.Raw:X8} "
            + $"impactRaw=0x{elem.EffectOnImpact.Raw:X8} deathRaw=0x{elem.EffectOnDeath.Raw:X8} emittedRaw=0x{elem.EffectEmitted.Raw:X8} extRaw=0x{elem.Extended.Raw:X8}");
        return elem;
    }

    private static void ResolveFxElemDefPointers(ref XFileReadContext context, FxElemDef elem)
    {
        var start = context.Position;
        Trace(
            ref context,
            $"elem children begin src=0x{start:X8} type=0x{elem.ElemType:X2} visualCount={elem.VisualCount}");
        var childStart = context.Position;
        ResolveVelSamples(ref context, elem.VelSamples, elem.VelIntervalCount + 1);
        TraceSpan(ref context, "velSamples", childStart);
        childStart = context.Position;
        ResolveVisSamples(ref context, elem.VisSamples, elem.VisStateIntervalCount + 1);
        TraceSpan(ref context, "visSamples", childStart);
        childStart = context.Position;
        ResolveVisuals(ref context, elem);
        TraceSpan(ref context, "visuals", childStart);
        childStart = context.Position;
        ResolveEffectDefRefPointer(ref context, elem.EffectOnImpact);
        TraceSpan(ref context, "effectImpact", childStart);
        childStart = context.Position;
        ResolveEffectDefRefPointer(ref context, elem.EffectOnDeath);
        TraceSpan(ref context, "effectDeath", childStart);
        childStart = context.Position;
        ResolveEffectDefRefPointer(ref context, elem.EffectEmitted);
        TraceSpan(ref context, "effectEmitted", childStart);
        childStart = context.Position;
        ResolveExtended(ref context, elem);
        TraceSpan(ref context, "extended", childStart);
        Trace(
            ref context,
            $"elem children end src=0x{start:X8} end=0x{context.Position:X8} len=0x{context.Position - start:X}");
    }

    private static ZonePointer<FxElemVisual[]> ReadFxElemVisualsField(ref XFileReadContext context, FxElemDef elem)
    {
        if (UsesVisualArrayPointer(elem))
            return context.ReadDirectPointer<FxElemVisual[]>("FxElemDef.Visuals");

        var pointer = new ZonePointer<FxElemVisual[]>(0);
        pointer.SetResult([ReadFxElemVisual(ref context, elem.ElemType, resolvePointers: false)]);
        return pointer;
    }

    private static void ResolveVisuals(ref XFileReadContext context, FxElemDef elem)
    {
        if (!UsesVisualArrayPointer(elem))
        {
            if (elem.Visuals.Result is { } visuals)
            {
                foreach (var visual in visuals)
                    ResolveFxElemVisual(ref context, elem.ElemType, visual);
            }

            return;
        }

        if (!elem.Visuals.IsInlineData)
        {
            elem.Visuals.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(elem.Visuals, (ref XFileReadContext pointerContext, ZonePointer<FxElemVisual[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref XFileReadContext valueContext) =>
                {
                    var visualCount = elem.VisualCount;
                    var visuals = new FxElemVisual[visualCount];

                    if (elem.ElemType == 0xB)
                    {
                        for (var i = 0; i < visuals.Length; i++)
                        {
                            visuals[i] = new FxElemVisual
                            {
                                DecalMaterial0 = MaterialReader.ReadMaterialPointerField(ref valueContext),
                                DecalMaterial1 = MaterialReader.ReadMaterialPointerField(ref valueContext),
                            };
                        }

                        foreach (var visual in visuals)
                            ResolveFxElemVisual(ref valueContext, elem.ElemType, visual);

                        return visuals;
                    }

                    for (var i = 0; i < visuals.Length; i++)
                        visuals[i] = ReadFxElemVisual(ref valueContext, elem.ElemType, resolvePointers: false);

                    foreach (var visual in visuals)
                        ResolveFxElemVisual(ref valueContext, elem.ElemType, visual);

                    return visuals;
                }));
        });
    }

    private static bool UsesVisualArrayPointer(FxElemDef elem)
    {
        return elem.ElemType == 0xB || elem.VisualCount > 1;
    }

    private static FxElemVisual ReadFxElemVisual(
        ref XFileReadContext context,
        byte elemType,
        bool resolvePointers = true)
    {
        var visual = new FxElemVisual();
        switch (elemType)
        {
            case 0x7:
                visual.Model = XModelReader.ReadXModelPointerField(ref context);
                if (resolvePointers)
                    XModelReader.ResolveXModelPointer(ref context, visual.Model);
                break;
            case 0xC:
                visual.EffectDef = ReadFxEffectDefRef(ref context, resolveName: resolvePointers);
                break;
            case 0xA:
                visual.SoundName = GenericReader.ReadStringPointer(ref context, resolve: resolvePointers);
                break;
            case 0x8:
            case 0x9:
                visual.Anonymous = context.ReadDirectPointer<FxUnknownVisual>("FxElemVisual.Anonymous");
                visual.Anonymous.SetResult(default);
                break;
            default:
                visual.Material = MaterialReader.ReadMaterialPointerField(ref context);
                if (resolvePointers)
                    MaterialReader.ResolveMaterialPointer(ref context, visual.Material);
                break;
        }

        return visual;
    }

    private static void ResolveFxElemVisual(
        ref XFileReadContext context,
        byte elemType,
        FxElemVisual visual)
    {
        switch (elemType)
        {
            case 0x7:
                XModelReader.ResolveXModelPointerNow(ref context, visual.Model);
                break;
            case 0xC:
                ResolveFxEffectDefRef(ref context, visual.EffectDef);
                break;
            case 0xA:
                GenericReader.ResolveStringPointerNow(ref context, visual.SoundName);
                break;
            case 0x8:
            case 0x9:
                visual.Anonymous.SetResult(default);
                break;
            case 0xB:
                MaterialReader.ResolveMaterialPointerNow(ref context, visual.DecalMaterial0);
                MaterialReader.ResolveMaterialPointerNow(ref context, visual.DecalMaterial1);
                break;
            default:
                MaterialReader.ResolveMaterialPointerNow(ref context, visual.Material);
                break;
        }
    }

    private static void ResolveEffectDefRefPointer(ref XFileReadContext context, ZonePointer<FxEffectDefRef> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxEffectDefRef> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) => ReadFxEffectDefRef(ref valueContext)));
        });
    }

    private static FxEffectDefRef ReadFxEffectDefRef(
        ref XFileReadContext context,
        bool resolveName = true)
    {
        var raw = context.ReadInt32();
        var handle = context.CreatePointer<FxEffectDef>(raw, register: false);
        var name = context.CreatePointer<string>(
            raw,
            resolutionKind: PointerResolutionKind.Direct,
            fieldPath: "FxEffectDefRef.Name");
        var reference = new FxEffectDefRef
        {
            Handle = handle,
            Name = name,
        };

        if (resolveName)
            context.ResolveInlinePointer(name, GenericReader.ReadStringPointerValue);

        handle.SetResult(default);
        return reference;
    }

    private static void ResolveFxEffectDefRef(
        ref XFileReadContext context,
        FxEffectDefRef reference)
    {
        GenericReader.ResolveStringPointerNow(ref context, reference.Name);
        reference.Handle.SetResult(default);
    }

    private static void ResolveExtended(ref XFileReadContext context, FxElemDef elem)
    {
        if (!elem.Extended.IsInlineData)
        {
            elem.Extended.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(elem.Extended, (ref XFileReadContext pointerContext, ZonePointer<FxElemExtendedDef> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref XFileReadContext valueContext) => ReadExtendedDef(ref valueContext, elem.ElemType)));
        });
    }

    private static FxElemExtendedDef ReadExtendedDef(ref XFileReadContext context, byte elemType)
    {
        return elemType switch
        {
            0x3 => new FxElemExtendedDef { TrailDef = ReadTrailDef(ref context) },
            0x6 => new FxElemExtendedDef { SparkFountainDef = ReadSparkFountainDef(ref context) },
            _ => new FxElemExtendedDef { UnknownDef = context.ReadByte() },
        };
    }

    private static FxTrailDef ReadTrailDef(ref XFileReadContext context)
    {
        var trail = new FxTrailDef
        {
            ScrollTimeMsec = context.ReadInt32(),
            RepeatDist = context.ReadInt32(),
            InvSplitDist = context.ReadFloat(),
            InvSplitArcDist = context.ReadFloat(),
            InvSplitTime = context.ReadFloat(),
            VertCount = context.ReadInt32(),
        };
        trail.Verts = context.ReadDirectPointer<FxTrailVertex[]>("FxTrailDef.Verts");
        trail.IndCount = context.ReadInt32();
        trail.Inds = context.ReadDirectPointer<ushort[]>("FxTrailDef.Inds");

        ResolveTrailVertices(ref context, trail.Verts, trail.VertCount);
        ResolveInlineUShorts(ref context, trail.Inds, trail.IndCount);

        return trail;
    }

    private static FxSparkFountainDef ReadSparkFountainDef(ref XFileReadContext context)
    {
        var start = context.Position;
        var spark = new FxSparkFountainDef
        {
            Gravity = context.ReadFloat(),
            BounceFrac = context.ReadFloat(),
            BounceRand = context.ReadFloat(),
            SparkSpacing = context.ReadFloat(),
            SparkLength = context.ReadFloat(),
            SparkCount = context.ReadInt32(),
            LoopTime = context.ReadFloat(),
            VelMin = context.ReadFloat(),
            VelMax = context.ReadFloat(),
            VelConeFrac = context.ReadFloat(),
            RestSpeed = context.ReadFloat(),
            BoostTime = context.ReadFloat(),
            BoostFactor = context.ReadFloat(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxSparkFountainDefSize)
            throw new InvalidDataException($"FxSparkFountainDef read {bytesRead:N0} bytes; expected {FxSparkFountainDefSize:N0} bytes.");

        return spark;
    }

    private static void ResolveVelSamples(ref XFileReadContext context, ZonePointer<FxElemVelStateSample[]> pointer, int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxElemVelStateSample[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var values = new FxElemVelStateSample[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = ReadVelStateSample(ref valueContext);

                    return values;
                }));
        });
    }

    private static void ResolveVisSamples(ref XFileReadContext context, ZonePointer<FxElemVisStateSample[]> pointer, int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxElemVisStateSample[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var values = new FxElemVisStateSample[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = ReadVisStateSample(ref valueContext);

                    return values;
                }));
        });
    }

    private static void ResolveTrailVertices(ref XFileReadContext context, ZonePointer<FxTrailVertex[]> pointer, int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxTrailVertex[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var values = new FxTrailVertex[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = ReadTrailVertex(ref valueContext);

                    return values;
                }));
        });
    }

    private static void ResolveInlineUShorts(ref XFileReadContext context, ZonePointer<ushort[]> pointer, int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<ushort[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var values = new ushort[count];
                    for (var i = 0; i < values.Length; i++)
                        values[i] = valueContext.ReadUInt16();

                    return values;
                }));
        });
    }

    private static FxSpawnDef ReadFxSpawnDef(ref XFileReadContext context)
    {
        return new FxSpawnDef
        {
            LoopingIntervalMsec = context.ReadInt32(),
            Count = context.ReadInt32(),
        };
    }

    private static FxIntRange ReadFxIntRange(ref XFileReadContext context)
    {
        return new FxIntRange
        {
            Base = context.ReadInt32(),
            Amplitude = context.ReadInt32(),
        };
    }

    private static FxFloatRange ReadFxFloatRange(ref XFileReadContext context)
    {
        return new FxFloatRange
        {
            Base = context.ReadFloat(),
            Amplitude = context.ReadFloat(),
        };
    }

    private static FxElemAtlas ReadFxElemAtlas(ref XFileReadContext context)
    {
        return new FxElemAtlas
        {
            Behavior = context.ReadByte(),
            Index = context.ReadByte(),
            Fps = context.ReadByte(),
            LoopCount = context.ReadByte(),
            ColIndexBits = context.ReadByte(),
            RowIndexBits = context.ReadByte(),
            EntryCount = unchecked((short)context.ReadUInt16()),
        };
    }

    private static FxElemVelStateSample ReadVelStateSample(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new FxElemVelStateSample
        {
            Local = ReadVelStateInFrame(ref context),
            World = ReadVelStateInFrame(ref context),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxElemVelStateSampleSize)
            throw new InvalidDataException($"FxElemVelStateSample read {bytesRead:N0} bytes; expected {FxElemVelStateSampleSize:N0} bytes.");

        return value;
    }

    private static FxElemVelStateInFrame ReadVelStateInFrame(ref XFileReadContext context)
    {
        return new FxElemVelStateInFrame
        {
            Velocity = ReadVec3Range(ref context),
            TotalDelta = ReadVec3Range(ref context),
        };
    }

    private static FxElemVec3Range ReadVec3Range(ref XFileReadContext context)
    {
        return new FxElemVec3Range
        {
            Base = ReadVec3(ref context),
            Amplitude = ReadVec3(ref context),
        };
    }

    private static FxElemVisStateSample ReadVisStateSample(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new FxElemVisStateSample
        {
            Base = ReadVisualState(ref context),
            Amplitude = ReadVisualState(ref context),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxElemVisStateSampleSize)
            throw new InvalidDataException($"FxElemVisStateSample read {bytesRead:N0} bytes; expected {FxElemVisStateSampleSize:N0} bytes.");

        return value;
    }

    private static FxElemVisualState ReadVisualState(ref XFileReadContext context)
    {
        return new FxElemVisualState
        {
            Color = new FxElemColor
            {
                R = context.ReadByte(),
                G = context.ReadByte(),
                B = context.ReadByte(),
                A = context.ReadByte(),
            },
            RotationDelta = context.ReadFloat(),
            RotationTotal = context.ReadFloat(),
            Size0 = context.ReadFloat(),
            Size1 = context.ReadFloat(),
            Scale = context.ReadFloat(),
        };
    }

    private static FxTrailVertex ReadTrailVertex(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new FxTrailVertex
        {
            Pos0 = context.ReadFloat(),
            Pos1 = context.ReadFloat(),
            Normal0 = context.ReadFloat(),
            Normal1 = context.ReadFloat(),
            TexCoord = context.ReadFloat(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxTrailVertexSize)
            throw new InvalidDataException($"FxTrailVertex read {bytesRead:N0} bytes; expected {FxTrailVertexSize:N0} bytes.");

        return value;
    }

    private static FastFile.Models.Utils.Bounds ReadBounds(ref XFileReadContext context)
    {
        return new FastFile.Models.Utils.Bounds
        {
            MidPoint = ReadVec3(ref context),
            HalfSize = ReadVec3(ref context),
        };
    }

    private static FastFile.Models.Utils.Vec3 ReadVec3(ref XFileReadContext context)
    {
        return new FastFile.Models.Utils.Vec3
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            Z = context.ReadFloat(),
        };
    }

    private static void TraceSpan(ref XFileReadContext context, string label, int start)
    {
        Trace(ref context, $"{label} src=0x{start:X8} end=0x{context.Position:X8} len=0x{context.Position - start:X}");
    }

    private static void Trace(ref XFileReadContext context, string message)
    {
        if (!TraceFx || !IsTraceOffset(context.Position) || _traceFxCount >= TraceFxLimit)
            return;

        Interlocked.Increment(ref _traceFxCount);
        Console.Error.WriteLine($"[fx-trace] asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] {message}");
    }

    private static bool IsTraceOffset(int offset)
    {
        return offset >= TraceFxStart && offset <= TraceFxEnd;
    }

    private static bool IsTraceEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }

    private static int GetTraceInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
        {
            return hex;
        }

        return int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
