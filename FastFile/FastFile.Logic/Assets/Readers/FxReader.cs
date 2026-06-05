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
    private const int FxTrailVertexSize = 24;
    private const int FxSparkFountainDefSize = 52;

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
        if (count <= 0)
            return [];

        var values = new FxElemDef[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadFxElemDef(ref context);

        foreach (var value in values)
            ResolveFxElemDefPointers(ref context, value);

        return values;
    }

    private static FxElemDef ReadFxElemDef(ref XFileReadContext context)
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
        elem.Visuals = context.ReadDirectPointer<FxElemVisual[]>("FxElemDef.Visuals");
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

        return elem;
    }

    private static void ResolveFxElemDefPointers(ref XFileReadContext context, FxElemDef elem)
    {
        ResolveVelSamples(ref context, elem.VelSamples, elem.VelIntervalCount);
        ResolveVisSamples(ref context, elem.VisSamples, elem.VisStateIntervalCount);
        ResolveVisuals(ref context, elem);
        ResolveEffectDefRefPointer(ref context, elem.EffectOnImpact);
        ResolveEffectDefRefPointer(ref context, elem.EffectOnDeath);
        ResolveEffectDefRefPointer(ref context, elem.EffectEmitted);
        ResolveExtended(ref context, elem);
    }

    private static void ResolveVisuals(ref XFileReadContext context, FxElemDef elem)
    {
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
                    var visualCount = elem.VisualCount == 1 ? 1 : elem.VisualCount;
                    var visuals = new FxElemVisual[visualCount];

                    if (elem.ElemType == 0xB)
                    {
                        for (var i = 0; i < visuals.Length; i++)
                        {
                            visuals[i] = new FxElemVisual
                            {
                                DecalMaterial0 = MaterialReader.ReadMaterialPointer(ref valueContext),
                                DecalMaterial1 = MaterialReader.ReadMaterialPointer(ref valueContext),
                            };
                        }

                        return visuals;
                    }

                    for (var i = 0; i < visuals.Length; i++)
                        visuals[i] = ReadFxElemVisual(ref valueContext, elem.ElemType);

                    return visuals;
                }));
        });
    }

    private static FxElemVisual ReadFxElemVisual(ref XFileReadContext context, byte elemType)
    {
        var visual = new FxElemVisual();
        switch (elemType)
        {
            case 0x7:
                visual.Model = XModelReader.ReadXModelPointer(ref context);
                break;
            case 0xC:
                visual.EffectDef = ReadFxEffectDefRef(ref context);
                break;
            case 0xA:
                visual.SoundName = GenericReader.ReadStringPointer(ref context);
                break;
            case 0x8:
            case 0x9:
                visual.Anonymous = context.ReadDirectPointer<FxUnknownVisual>("FxElemVisual.Anonymous");
                visual.Anonymous.SetResult(default);
                break;
            default:
                visual.Material = MaterialReader.ReadMaterialPointer(ref context);
                break;
        }

        return visual;
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
                ReadFxEffectDefRef));
        });
    }

    private static FxEffectDefRef ReadFxEffectDefRef(ref XFileReadContext context)
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

        context.ResolveInlinePointer(name, GenericReader.ReadStringPointerValue);

        handle.SetResult(default);
        return reference;
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
            AlignmentPadding = context.ReadInt32(),
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
}
