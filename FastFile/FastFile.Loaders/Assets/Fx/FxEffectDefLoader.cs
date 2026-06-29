using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.XModel;
using FastFile.Models.Assets.Fx;
using FastFile.Models.Assets.Material;
using XModelAssetModel = FastFile.Models.Assets.XModel.XModelAsset;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Fx;

public sealed class FxEffectDefLoader
{
    private readonly MaterialLoader _materialLoader = new();
    private readonly XModelLoader _xmodelLoader = new();

    public FxEffectDefAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level Fx pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        return LoadInlineOrInsert(cursor, pointer, context);
    }

    public FxEffectDefAsset? LoadFromPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<FxEffectDefAsset>(pointer, context, FxEffectDefAsset.SerializedSize, "FxEffectDef"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<FxEffectDefAsset>(pointer, FxEffectDefAsset.SerializedSize, "FxEffectDef");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new NotSupportedException($"FxEffectDef pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        return LoadInlineOrInsert(cursor, pointer, context);
    }

    private FxEffectDefAsset LoadInlineOrInsert(
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
            FxEffectDefAsset effect = ReadFxEffectDef(cursor, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return effect;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private FxEffectDefAsset ReadFxEffectDef(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, FxEffectDefAsset.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XString namePointer = ReadXStringPointer(rootCursor);
        int flags = rootCursor.ReadInt32();
        int totalSize = rootCursor.ReadInt32();
        int msecLoopingLife = rootCursor.ReadInt32();
        int elemDefCountLooping = rootCursor.ReadInt32();
        int elemDefCountOneShot = rootCursor.ReadInt32();
        int elemDefCountEmission = rootCursor.ReadInt32();
        XPointer<FxElemDef[]> elemDefsPointer = ReadPointer<FxElemDef[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != FxEffectDefAsset.SerializedSize)
            throw new InvalidDataException($"FxEffectDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{FxEffectDefAsset.SerializedSize:X}.");

        int elemDefCount = checked(elemDefCountLooping + elemDefCountOneShot + elemDefCountEmission);
        context.Diagnostics.Trace(
            $"  FxEffectDef root source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} flags=0x{flags:X8} totalSize={totalSize} " +
            $"counts={elemDefCountLooping}/{elemDefCountOneShot}/{elemDefCountEmission} elemDefs=0x{elemDefsPointer.Raw:X8} " +
            $"blocks={context.Blocks.DescribePositions()}");

        string? name;
        IReadOnlyList<FxElemDef> elemDefs;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = ReadXString(cursor, namePointer, context);
            elemDefs = ReadFxElemDefArray(cursor, elemDefsPointer.Untyped, elemDefCount, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return new FxEffectDefAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            Flags = flags,
            TotalSize = totalSize,
            MsecLoopingLife = msecLoopingLife,
            ElemDefCountLooping = elemDefCountLooping,
            ElemDefCountOneShot = elemDefCountOneShot,
            ElemDefCountEmission = elemDefCountEmission,
            ElemDefsPointer = elemDefsPointer,
            ElemDefs = elemDefs
        };
    }

    private IReadOnlyList<FxElemDef> ReadFxElemDefArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative FxElemDef count {count}.");

        if (pointer.Type == PointerType.Null || count == 0)
            return [];

        XBlockAddress elemAddress = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] elemBytes = context.Blocks.Load(cursor, checked(count * FxElemDef.SerializedSize));
        var elemCursor = new FastFileCursor(elemBytes, elemAddress);

        var roots = new FxElemDefRoot[count];
        for (int i = 0; i < roots.Length; i++)
            roots[i] = ReadFxElemDefRoot(elemCursor);

        var elems = new FxElemDef[count];
        for (int i = 0; i < elems.Length; i++)
            elems[i] = ReadFxElemDefChildren(cursor, roots[i], context);

        context.Diagnostics.Trace(
            $"    FxElemDef[] sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} count={count} target={elemAddress} " +
            $"blocks={context.Blocks.DescribePositions()}");

        return elems;
    }

    private static FxElemDefRoot ReadFxElemDefRoot(FastFileCursor cursor)
    {
        int offset = cursor.AddressAt(cursor.Offset)?.Offset ?? cursor.Offset;
        int start = cursor.Offset;
        int flags = cursor.ReadInt32();
        FxSpawnDef spawn = ReadFxSpawnDef(cursor);
        FxFloatRange spawnRange = ReadFxFloatRange(cursor);
        FxFloatRange fadeInRange = ReadFxFloatRange(cursor);
        FxFloatRange fadeOutRange = ReadFxFloatRange(cursor);
        float spawnFrustumCullRadius = ReadSingle(cursor);
        FxIntRange spawnDelayMsec = ReadFxIntRange(cursor);
        FxIntRange lifeSpanMsec = ReadFxIntRange(cursor);
        IReadOnlyList<FxFloatRange> spawnOrigin = ReadFxFloatRanges(cursor, 3);
        FxFloatRange spawnOffsetRadius = ReadFxFloatRange(cursor);
        FxFloatRange spawnOffsetHeight = ReadFxFloatRange(cursor);
        IReadOnlyList<FxFloatRange> spawnAngles = ReadFxFloatRanges(cursor, 3);
        IReadOnlyList<FxFloatRange> angularVelocity = ReadFxFloatRanges(cursor, 3);
        FxFloatRange initialRotation = ReadFxFloatRange(cursor);
        FxFloatRange gravity = ReadFxFloatRange(cursor);
        FxFloatRange reflectionFactor = ReadFxFloatRange(cursor);
        FxElemAtlas atlas = ReadFxElemAtlas(cursor);
        var elemType = (FxElemType)cursor.ReadByte();
        byte visualCount = cursor.ReadByte();
        byte velIntervalCount = cursor.ReadByte();
        byte visStateIntervalCount = cursor.ReadByte();
        XPointer<FxElemVelStateSample[]> velSamplesPointer = ReadPointer<FxElemVelStateSample[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxElemVisStateSample[]> visSamplesPointer = ReadPointer<FxElemVisStateSample[]>(cursor, XPointerResolutionMode.Direct);
        FxElemDefVisualsRoot visuals = ReadFxElemDefVisualsRoot(cursor);
        Bounds collBounds = ReadBounds(cursor);
        FxEffectDefRef effectOnImpact = ReadFxEffectDefRefRoot(cursor);
        FxEffectDefRef effectOnDeath = ReadFxEffectDefRefRoot(cursor);
        FxEffectDefRef effectEmitted = ReadFxEffectDefRefRoot(cursor);
        FxFloatRange emitDist = ReadFxFloatRange(cursor);
        FxFloatRange emitDistVariance = ReadFxFloatRange(cursor);
        XPointer<FxElemExtendedDef> extendedPointer = ReadPointer<FxElemExtendedDef>(cursor, XPointerResolutionMode.Direct);
        byte sortOrder = cursor.ReadByte();
        byte lightingFrac = cursor.ReadByte();
        byte useItemClip = cursor.ReadByte();
        byte fadeInfo = cursor.ReadByte();

        if (cursor.Offset - start != FxElemDef.SerializedSize)
            throw new InvalidDataException($"FxElemDef consumed 0x{cursor.Offset - start:X} bytes instead of 0x{FxElemDef.SerializedSize:X}.");

        return new FxElemDefRoot(
            offset,
            flags,
            spawn,
            spawnRange,
            fadeInRange,
            fadeOutRange,
            spawnFrustumCullRadius,
            spawnDelayMsec,
            lifeSpanMsec,
            spawnOrigin,
            spawnOffsetRadius,
            spawnOffsetHeight,
            spawnAngles,
            angularVelocity,
            initialRotation,
            gravity,
            reflectionFactor,
            atlas,
            elemType,
            visualCount,
            velIntervalCount,
            visStateIntervalCount,
            velSamplesPointer,
            visSamplesPointer,
            visuals,
            collBounds,
            effectOnImpact,
            effectOnDeath,
            effectEmitted,
            emitDist,
            emitDistVariance,
            extendedPointer,
            sortOrder,
            lightingFrac,
            useItemClip,
            fadeInfo);
    }

    private FxElemDef ReadFxElemDefChildren(
        FastFileCursor cursor,
        FxElemDefRoot root,
        FastFileLoadContext context)
    {
        IReadOnlyList<FxElemVelStateSample> velSamples = ReadFxElemVelStateSamples(
            cursor,
            root.VelSamplesPointer.Untyped,
            root.VelIntervalCount + 1,
            context);
        IReadOnlyList<FxElemVisStateSample> visSamples = ReadFxElemVisStateSamples(
            cursor,
            root.VisSamplesPointer.Untyped,
            root.VisStateIntervalCount + 1,
            context);
        FxVisualPayload visuals = ReadFxElemDefVisuals(cursor, root.Visuals, root.ElemType, root.VisualCount, context);
        FxEffectDefRef effectOnImpact = ReadFxEffectDefRef(cursor, root.EffectOnImpact, context);
        FxEffectDefRef effectOnDeath = ReadFxEffectDefRef(cursor, root.EffectOnDeath, context);
        FxEffectDefRef effectEmitted = ReadFxEffectDefRef(cursor, root.EffectEmitted, context);
        FxElemExtendedDef? extended = ReadFxElemExtended(cursor, root.ExtendedPointer.Untyped, root.ElemType, context);

        context.Diagnostics.Trace(
            $"      FxElemDef offset=0x{root.Offset:X} type={(byte)root.ElemType}({root.ElemType}) visualCount={root.VisualCount} " +
            $"vel={root.VelSamplesPointer.Raw:X8} vis={root.VisSamplesPointer.Raw:X8} visuals={root.Visuals.Raw.Raw:X8} " +
            $"extended={root.ExtendedPointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        return new FxElemDef
        {
            Offset = root.Offset,
            Flags = root.Flags,
            Spawn = root.Spawn,
            SpawnRange = root.SpawnRange,
            FadeInRange = root.FadeInRange,
            FadeOutRange = root.FadeOutRange,
            SpawnFrustumCullRadius = root.SpawnFrustumCullRadius,
            SpawnDelayMsec = root.SpawnDelayMsec,
            LifeSpanMsec = root.LifeSpanMsec,
            SpawnOrigin = root.SpawnOrigin,
            SpawnOffsetRadius = root.SpawnOffsetRadius,
            SpawnOffsetHeight = root.SpawnOffsetHeight,
            SpawnAngles = root.SpawnAngles,
            AngularVelocity = root.AngularVelocity,
            InitialRotation = root.InitialRotation,
            Gravity = root.Gravity,
            ReflectionFactor = root.ReflectionFactor,
            Atlas = root.Atlas,
            ElemType = root.ElemType,
            VisualCount = root.VisualCount,
            VelIntervalCount = root.VelIntervalCount,
            VisStateIntervalCount = root.VisStateIntervalCount,
            VelSamplesPointer = root.VelSamplesPointer,
            VelSamples = velSamples,
            VisSamplesPointer = root.VisSamplesPointer,
            VisSamples = visSamples,
            Visuals = visuals.InlineVisual ?? new FxElemDefVisuals { Offset = root.Visuals.Offset },
            VisualArrayPointer = visuals.VisualArrayPointer,
            VisualArray = visuals.VisualArray,
            MarkVisualArrayPointer = visuals.MarkVisualArrayPointer,
            MarkVisualArray = visuals.MarkVisualArray,
            CollBounds = root.CollBounds,
            EffectOnImpact = effectOnImpact,
            EffectOnDeath = effectOnDeath,
            EffectEmitted = effectEmitted,
            EmitDist = root.EmitDist,
            EmitDistVariance = root.EmitDistVariance,
            ExtendedPointer = root.ExtendedPointer,
            Extended = extended,
            SortOrder = root.SortOrder,
            LightingFrac = root.LightingFrac,
            UseItemClip = root.UseItemClip,
            FadeInfo = root.FadeInfo
        };
    }

    private static IReadOnlyList<FxElemVelStateSample> ReadFxElemVelStateSamples(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || count <= 0)
            return [];

        XBlockAddress address = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] bytes = context.Blocks.Load(cursor, checked(count * 0x60));
        var sampleCursor = new FastFileCursor(bytes, address);
        var samples = new FxElemVelStateSample[count];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = new FxElemVelStateSample(ReadFxElemVelStateInFrame(sampleCursor), ReadFxElemVelStateInFrame(sampleCursor));
        return samples;
    }

    private static IReadOnlyList<FxElemVisStateSample> ReadFxElemVisStateSamples(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || count <= 0)
            return [];

        XBlockAddress address = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] bytes = context.Blocks.Load(cursor, checked(count * 0x30));
        var sampleCursor = new FastFileCursor(bytes, address);
        var samples = new FxElemVisStateSample[count];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = new FxElemVisStateSample(ReadFxElemVisualState(sampleCursor), ReadFxElemVisualState(sampleCursor));
        return samples;
    }

    private FxVisualPayload ReadFxElemDefVisuals(
        FastFileCursor cursor,
        FxElemDefVisualsRoot inlineVisual,
        FxElemType elemType,
        byte visualCount,
        FastFileLoadContext context)
    {
        if (elemType == FxElemType.Decal)
        {
            XPointer<FxElemMarkVisuals[]> markPointer = ReinterpretPointer<FxElemMarkVisuals[]>(inlineVisual.Raw, XPointerResolutionMode.Direct);
            return new FxVisualPayload(null, null, [], markPointer, ReadFxElemMarkVisualArray(cursor, markPointer.Untyped, visualCount, context));
        }

        if (visualCount > 1)
        {
            XPointer<FxElemDefVisuals[]> visualPointer = ReinterpretPointer<FxElemDefVisuals[]>(inlineVisual.Raw, XPointerResolutionMode.Direct);
            return new FxVisualPayload(null, visualPointer, ReadFxElemVisualArray(cursor, visualPointer.Untyped, elemType, visualCount, context), null, []);
        }

        return new FxVisualPayload(ReadFxElemVisual(cursor, inlineVisual, elemType, context), null, [], null, []);
    }

    private IReadOnlyList<FxElemDefVisuals> ReadFxElemVisualArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        FxElemType elemType,
        int visualCount,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || visualCount <= 0)
            return [];

        XBlockAddress visualAddress = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] visualBytes = context.Blocks.Load(cursor, checked(visualCount * FxElemDefVisuals.SerializedSize));
        var visualCursor = new FastFileCursor(visualBytes, visualAddress);
        var visuals = new FxElemDefVisuals[visualCount];
        for (int i = 0; i < visuals.Length; i++)
            visuals[i] = ReadFxElemVisual(cursor, ReadFxElemDefVisualsRoot(visualCursor), elemType, context);

        return visuals;
    }

    private IReadOnlyList<FxElemMarkVisuals> ReadFxElemMarkVisualArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int visualCount,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || visualCount <= 0)
            return [];

        XBlockAddress markAddress = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] markBytes = context.Blocks.Load(cursor, checked(visualCount * FxElemMarkVisuals.SerializedSize));
        var markCursor = new FastFileCursor(markBytes, markAddress);
        var marks = new FxElemMarkVisuals[visualCount];
        for (int i = 0; i < marks.Length; i++)
        {
            int offset = markCursor.AddressAt(markCursor.Offset)?.Offset ?? markCursor.Offset;
            XPointer<MaterialAsset> material0Pointer = ReadPointer<MaterialAsset>(markCursor, XPointerResolutionMode.AliasCell);
            XPointer<MaterialAsset> material1Pointer = ReadPointer<MaterialAsset>(markCursor, XPointerResolutionMode.AliasCell);
            MaterialAsset? material0 = ReadMaterialPointer(cursor, material0Pointer.Untyped, context);
            MaterialAsset? material1 = ReadMaterialPointer(cursor, material1Pointer.Untyped, context);
            marks[i] = new FxElemMarkVisuals
            {
                Offset = offset,
                Material0Pointer = material0Pointer,
                Material0 = material0,
                Material1Pointer = material1Pointer,
                Material1 = material1
            };
        }

        return marks;
    }

    private FxElemDefVisuals ReadFxElemVisual(
        FastFileCursor cursor,
        FxElemDefVisualsRoot visual,
        FxElemType elemType,
        FastFileLoadContext context)
    {
        switch (elemType)
        {
            case FxElemType.Model:
            {
                XPointer<XModelAssetModel> modelPointer = ReinterpretPointer<XModelAssetModel>(visual.Raw, XPointerResolutionMode.AliasCell);
                XModelAssetModel? model = ReadXModelPointer(cursor, modelPointer.Untyped, context);
                return new FxElemDefVisuals
                {
                    Offset = visual.Offset,
                    Visual = new FxModelVisual
                    {
                        ModelPointer = modelPointer,
                        Model = model
                    }
                };
            }

            case FxElemType.OmniLight:
            case FxElemType.SpotLight:
                return new FxElemDefVisuals
                {
                    Offset = visual.Offset,
                    Visual = new FxNoChildVisual { Reserved = visual.Raw.Raw }
                };

            case FxElemType.Sound:
            {
                XString soundPointer = ReinterpretPointer<string>(visual.Raw, XPointerResolutionMode.Direct);
                string? soundName = ReadXString(cursor, soundPointer, context);
                return new FxElemDefVisuals
                {
                    Offset = visual.Offset,
                    Visual = new FxSoundVisual
                    {
                        SoundNamePointer = soundPointer,
                        SoundName = soundName
                    }
                };
            }

            case FxElemType.Runner:
            {
                var effectRef = ReadFxEffectDefRef(cursor, new FxEffectDefRef { NamePointer = ReinterpretPointer<string>(visual.Raw, XPointerResolutionMode.Direct) }, context);
                return new FxElemDefVisuals
                {
                    Offset = visual.Offset,
                    Visual = new FxEffectVisual { EffectDef = effectRef }
                };
            }

            default:
            {
                XPointer<MaterialAsset> materialPointer = ReinterpretPointer<MaterialAsset>(visual.Raw, XPointerResolutionMode.AliasCell);
                MaterialAsset? material = ReadMaterialPointer(cursor, materialPointer.Untyped, context);
                return new FxElemDefVisuals
                {
                    Offset = visual.Offset,
                    Visual = new FxMaterialVisual
                    {
                        MaterialPointer = materialPointer,
                        Material = material
                    }
                };
            }
        }
    }

    private static FxEffectDefRef ReadFxEffectDefRef(
        FastFileCursor cursor,
        FxEffectDefRef effectRef,
        FastFileLoadContext context)
    {
        return new FxEffectDefRef
        {
            NamePointer = effectRef.NamePointer,
            Name = ReadXString(cursor, effectRef.NamePointer, context)
        };
    }

    private static FxElemExtendedDef? ReadFxElemExtended(
        FastFileCursor cursor,
        XPointerReference pointer,
        FxElemType elemType,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        return elemType switch
        {
            FxElemType.Trail => new FxElemExtendedDef
            {
                Kind = FxElemExtendedDefKind.Trail,
                TrailDef = ReadFxTrailDef(cursor, pointer, context)
            },
            FxElemType.SparkFountain => new FxElemExtendedDef
            {
                Kind = FxElemExtendedDefKind.SparkFountain,
                SparkFountainDef = ReadFxSparkFountainDef(cursor, pointer, context)
            },
            _ => new FxElemExtendedDef
            {
                Kind = FxElemExtendedDefKind.DefaultBytePayload,
                DefaultBytePayload = ReadFxExtendedDefaultByte(cursor, pointer, context)
            }
        };
    }

    private static FxTrailDef ReadFxTrailDef(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        XBlockAddress trailAddress = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] trailBytes = context.Blocks.Load(cursor, FxTrailDef.SerializedSize);
        var trailCursor = new FastFileCursor(trailBytes, trailAddress);

        int scrollTimeMsec = trailCursor.ReadInt32();
        int repeatDist = trailCursor.ReadInt32();
        float invSplitDist = ReadSingle(trailCursor);
        float invSplitArcDist = ReadSingle(trailCursor);
        float invSplitTime = ReadSingle(trailCursor);
        int vertCount = trailCursor.ReadInt32();
        XPointer<FxTrailVertex[]> vertsPointer = ReadPointer<FxTrailVertex[]>(trailCursor, XPointerResolutionMode.Direct);
        int indCount = trailCursor.ReadInt32();
        XPointer<ushort[]> indsPointer = ReadPointer<ushort[]>(trailCursor, XPointerResolutionMode.Direct);

        if (trailCursor.Offset != FxTrailDef.SerializedSize)
            throw new InvalidDataException($"FxTrailDef consumed 0x{trailCursor.Offset:X} bytes instead of 0x{FxTrailDef.SerializedSize:X}.");

        return new FxTrailDef
        {
            ScrollTimeMsec = scrollTimeMsec,
            RepeatDist = repeatDist,
            InvSplitDist = invSplitDist,
            InvSplitArcDist = invSplitArcDist,
            InvSplitTime = invSplitTime,
            VertCount = vertCount,
            VertsPointer = vertsPointer,
            Verts = ReadFxTrailVerts(cursor, vertsPointer.Untyped, vertCount, context),
            IndCount = indCount,
            IndsPointer = indsPointer,
            Inds = ReadUInt16Payload(cursor, indsPointer.Untyped, indCount, alignment: 2, context)
        };
    }

    private static FxSparkFountainDef ReadFxSparkFountainDef(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] bytes = context.Blocks.Load(cursor, FxSparkFountainDef.SerializedSize);
        var c = new FastFileCursor(bytes);
        return new FxSparkFountainDef(
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            c.ReadInt32(),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c),
            ReadSingle(c));
    }

    private static byte ReadFxExtendedDefaultByte(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        PatchNonNullCurrentPointerCell(pointer, alignment: 1, context);
        return context.Blocks.Load(cursor, 1)[0];
    }

    private static IReadOnlyList<FxTrailVertex> ReadFxTrailVerts(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || count <= 0)
            return [];

        XBlockAddress address = PatchNonNullCurrentPointerCell(pointer, alignment: 4, context);
        byte[] bytes = context.Blocks.Load(cursor, checked(count * FxTrailDef.VertexSerializedSize));
        var c = new FastFileCursor(bytes, address);
        var verts = new FxTrailVertex[count];
        for (int i = 0; i < verts.Length; i++)
            verts[i] = new FxTrailVertex(ReadSingle(c), ReadSingle(c), ReadSingle(c), ReadSingle(c), ReadSingle(c));
        return verts;
    }

    private static IReadOnlyList<ushort> ReadUInt16Payload(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null || count <= 0)
            return [];

        XBlockAddress address = PatchNonNullCurrentPointerCell(pointer, alignment, context);
        byte[] bytes = context.Blocks.Load(cursor, checked(count * sizeof(ushort)));
        var c = new FastFileCursor(bytes, address);
        var values = new ushort[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = c.ReadUInt16();
        return values;
    }

    private MaterialAsset? ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<MaterialAsset>(pointer, context, MaterialAsset.SerializedSize, "Material"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        return _materialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private XModelAssetModel? ReadXModelPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset<XModelAssetModel>(pointer, context, XModelAssetModel.SerializedSize, "XModel"))
            return null;

        return _xmodelLoader.LoadFromPointer(cursor, pointer, context);
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

    private static XBlockAddress PatchNonNullCurrentPointerCell(
        XPointerReference pointer,
        int alignment,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            throw new InvalidDataException("Cannot patch a null Fx pointer cell to the current stream position.");

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Pointer 0x{pointer.Raw:X8} has no destination cell address to patch.");

        if (alignment > 1)
            context.Blocks.AlignCurrent(alignment);

        XBlockAddress targetAddress = context.Blocks.CurrentAddress;
        context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(targetAddress));
        return targetAddress;
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

    private static XPointer<T> ReinterpretPointer<T>(
        XPointer<object> pointer,
        XPointerResolutionMode mode)
    {
        return new XPointer<T>(pointer.Raw, mode, pointer.CellAddress);
    }

    private static XString ReadXStringPointer(FastFileCursor cursor)
    {
        return ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static FxElemDefVisualsRoot ReadFxElemDefVisualsRoot(FastFileCursor cursor)
    {
        int offset = cursor.AddressAt(cursor.Offset)?.Offset ?? cursor.Offset;
        return new FxElemDefVisualsRoot(offset, ReadPointer<object>(cursor, XPointerResolutionMode.Direct));
    }

    private static FxEffectDefRef ReadFxEffectDefRefRoot(FastFileCursor cursor)
    {
        return new FxEffectDefRef
        {
            NamePointer = ReadXStringPointer(cursor)
        };
    }

    private static FxIntRange ReadFxIntRange(FastFileCursor cursor)
    {
        return new FxIntRange(cursor.ReadInt32(), cursor.ReadInt32());
    }

    private static FxSpawnDef ReadFxSpawnDef(FastFileCursor cursor)
    {
        return new FxSpawnDef(cursor.ReadInt32(), cursor.ReadInt32());
    }

    private static FxFloatRange ReadFxFloatRange(FastFileCursor cursor)
    {
        return new FxFloatRange(ReadSingle(cursor), ReadSingle(cursor));
    }

    private static IReadOnlyList<FxFloatRange> ReadFxFloatRanges(
        FastFileCursor cursor,
        int count)
    {
        var ranges = new FxFloatRange[count];
        for (int i = 0; i < ranges.Length; i++)
            ranges[i] = ReadFxFloatRange(cursor);
        return ranges;
    }

    private static FxElemAtlas ReadFxElemAtlas(FastFileCursor cursor)
    {
        return new FxElemAtlas(
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            cursor.ReadByte(),
            unchecked((short)cursor.ReadUInt16()));
    }

    private static Bounds ReadBounds(FastFileCursor cursor)
    {
        return new Bounds(ReadVec3(cursor), ReadVec3(cursor));
    }

    private static Vec3 ReadVec3(FastFileCursor cursor)
    {
        return new Vec3(ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor));
    }

    private static FxElemVelStateInFrame ReadFxElemVelStateInFrame(FastFileCursor cursor)
    {
        return new FxElemVelStateInFrame(
            new FxElemVec3Range(ReadVec3(cursor), ReadVec3(cursor)),
            new FxElemVec3Range(ReadVec3(cursor), ReadVec3(cursor)));
    }

    private static FxElemVisualState ReadFxElemVisualState(FastFileCursor cursor)
    {
        return new FxElemVisualState(
            new FxElemColor(cursor.ReadByte(), cursor.ReadByte(), cursor.ReadByte(), cursor.ReadByte()),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor),
            ReadSingle(cursor));
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private sealed record FxVisualPayload(
        FxElemDefVisuals? InlineVisual,
        XPointer<FxElemDefVisuals[]>? VisualArrayPointer,
        IReadOnlyList<FxElemDefVisuals> VisualArray,
        XPointer<FxElemMarkVisuals[]>? MarkVisualArrayPointer,
        IReadOnlyList<FxElemMarkVisuals> MarkVisualArray);

    private sealed record FxElemDefVisualsRoot(
        int Offset,
        XPointer<object> Raw);

    private sealed record FxElemDefRoot(
        int Offset,
        int Flags,
        FxSpawnDef Spawn,
        FxFloatRange SpawnRange,
        FxFloatRange FadeInRange,
        FxFloatRange FadeOutRange,
        float SpawnFrustumCullRadius,
        FxIntRange SpawnDelayMsec,
        FxIntRange LifeSpanMsec,
        IReadOnlyList<FxFloatRange> SpawnOrigin,
        FxFloatRange SpawnOffsetRadius,
        FxFloatRange SpawnOffsetHeight,
        IReadOnlyList<FxFloatRange> SpawnAngles,
        IReadOnlyList<FxFloatRange> AngularVelocity,
        FxFloatRange InitialRotation,
        FxFloatRange Gravity,
        FxFloatRange ReflectionFactor,
        FxElemAtlas Atlas,
        FxElemType ElemType,
        byte VisualCount,
        byte VelIntervalCount,
        byte VisStateIntervalCount,
        XPointer<FxElemVelStateSample[]> VelSamplesPointer,
        XPointer<FxElemVisStateSample[]> VisSamplesPointer,
        FxElemDefVisualsRoot Visuals,
        Bounds CollBounds,
        FxEffectDefRef EffectOnImpact,
        FxEffectDefRef EffectOnDeath,
        FxEffectDefRef EffectEmitted,
        FxFloatRange EmitDist,
        FxFloatRange EmitDistVariance,
        XPointer<FxElemExtendedDef> ExtendedPointer,
        byte SortOrder,
        byte LightingFrac,
        byte UseItemClip,
        byte FadeInfo);
}
