using System.Buffers.Binary;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;
using XModelAsset = FastFile.Models.Assets.XModels.XModel;

namespace FastFile.Logic.Assets.Readers;

public sealed class FxAssetReader : XAssetReadHandler
{
    private static readonly bool TraceFxEnabled = Environment.GetEnvironmentVariable("FF_TRACE_FX") == "1";

    private static readonly XPointerFieldAttribute FxElemDefArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxEffectDef.ElemDefCount)
    };

    private static readonly XPointerFieldAttribute FxElemVelSamplesAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxElemDef.VelSampleCount)
    };

    private static readonly XPointerFieldAttribute FxElemVisSamplesAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxElemDef.VisStateSampleCount)
    };

    private static readonly XPointerFieldAttribute FxElemVisualArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxElemDef.VisualCountValue)
    };

    private static readonly XPointerFieldAttribute FxElemMarkVisualArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxElemDef.VisualCountValue)
    };

    private static readonly XPointerFieldAttribute MaterialWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute XModelWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute CStringAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString
    };

    private static readonly XPointerFieldAttribute FxTrailDefAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4
    };

    private static readonly XPointerFieldAttribute FxSparkFountainDefAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4
    };

    private static readonly XPointerFieldAttribute FxExtendedUnknownAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 1
    };

    private static readonly XPointerFieldAttribute FxTrailVertsAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FxTrailDef.VertCount)
    };

    private static readonly XPointerFieldAttribute FxTrailIndsAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(FxTrailDef.IndCount)
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case FxEffectDef effect:
                Load_FxEffectDef(effect, context);
                return true;

            case FxElemDef elem:
                Load_FxElemDef(elem, context);
                return true;

            case FxEffectDefRef effectRef:
                Load_FxEffectDefRef(effectRef, context);
                return true;

            case FxElemMarkVisuals markVisuals:
                Load_FxElemMarkVisuals(markVisuals, context);
                return true;

            case FxTrailDef trailDef:
                Load_FxTrailDef(trailDef, context);
                return true;

            default:
                return false;
        }
    }

    // PS3 0x113038 / Xbox Load_FxEffectDef
    private static void Load_FxEffectDef(
        FxEffectDef effect,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(effect, nameof(FxEffectDef.NamePtr));
            TraceFxEffect("begin", effect);
            ResolveNonNullCurrentStreamPointer(effect.ElemDefs, context, FxElemDefArrayAttribute, effect);
            TraceFxEffect("end", effect);
        });
    }

    // PS3 0x112e20 / Xbox Load_FxElemDef
    private static void Load_FxElemDef(
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        TraceFxElement("begin", elem, context);
        ResolveNonNullCurrentStreamPointer(elem.VelSamples, context, FxElemVelSamplesAttribute, elem);
        ResolveNonNullCurrentStreamPointer(elem.VisSamples, context, FxElemVisSamplesAttribute, elem);
        Load_FxElemDefVisuals(elem, context);

        TraceFxElement("post-visuals", elem, context);
        Load_FxEffectDefRef(elem.EffectOnImpact, context);
        Load_FxEffectDefRef(elem.EffectOnDeath, context);
        Load_FxEffectDefRef(elem.EffectEmitted, context);

        TraceFxElement("post-refs", elem, context);
        Load_FxElemExtendedDefPtr(elem, context);
        TraceFxElement("end", elem, context);
    }

    // PS3 0x105a88 / Xbox Load_FxEffectDefRef
    private static void Load_FxEffectDefRef(
        FxEffectDefRef effectRef,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(effectRef, nameof(FxEffectDefRef.NamePtr));
    }

    // PS3 0x112d20 / 0x112ba0 / 0x1105e0 branch family.
    private static void Load_FxElemDefVisuals(
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        TraceFxVisualDispatch(elem);

        if (elem.ElemType == FxElemType.Decal)
        {
            elem.MarkVisualArray = XPointerCodec.ReinterpretPointer<FxElemMarkVisuals[]>(
                elem.Visuals.Raw,
                PointerResolutionKind.Direct);

            ResolveNonNullCurrentStreamPointer(elem.MarkVisualArray, context, FxElemMarkVisualArrayAttribute, elem);
            return;
        }

        if (elem.VisualCount > 1)
        {
            elem.VisualArray = XPointerCodec.ReinterpretPointer<FxElemDefVisuals[]>(
                elem.Visuals.Raw,
                PointerResolutionKind.Direct);

            ResolveNonNullCurrentStreamPointer(elem.VisualArray, context, FxElemVisualArrayAttribute, elem);

            if (elem.VisualArray.Value is not null)
            {
                foreach (var visual in elem.VisualArray.Value)
                    Load_FxElemDefVisualsUnion(visual, elem.ElemType, context);
            }

            return;
        }

        Load_FxElemDefVisualsUnion(elem.Visuals, elem.ElemType, context);
    }

    private static void Load_FxElemDefVisualsUnion(
        FxElemDefVisuals visual,
        FxElemType elemType,
        IXAssetReaderContext context)
    {
        TraceFxVisualUnion("dispatch", elemType, visual.Raw.Raw, context);

        switch (elemType)
        {
            case FxElemType.Model:
                TraceFxVisualUnion("model", elemType, visual.Raw.Raw, context);
                visual.Model = XPointerCodec.ReinterpretPointer<XModelAsset>(
                    visual.Raw,
                    PointerResolutionKind.Alias);

                context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
                {
                    context.ResolvePointerValue(visual.Model, XModelWrapperAttribute, visual);
                });
                break;

            case FxElemType.OmniLight:
            case FxElemType.SpotLight:
                // PS3 0x112ba0 returns without loading child data for these two elem types.
                TraceFxVisualUnion("no-child", elemType, visual.Raw.Raw, context);
                break;

            case FxElemType.Sound:
                TraceFxVisualUnion("sound", elemType, visual.Raw.Raw, context);
                visual.SoundName = XPointerCodec.ReinterpretPointer<string>(
                    visual.Raw,
                    PointerResolutionKind.Direct);
                context.ResolvePointerValue(visual.SoundName, CStringAttribute, visual);
                break;

            case FxElemType.Runner:
                TraceFxVisualUnion("runner", elemType, visual.Raw.Raw, context);
                visual.EffectDef = new FxEffectDefRef
                {
                    NamePtr = XPointerCodec.ReinterpretPointer<string>(
                        visual.Raw,
                        PointerResolutionKind.Direct)
                };
                Load_FxEffectDefRef(visual.EffectDef, context);
                break;

            default:
                TraceFxVisualUnion("material", elemType, visual.Raw.Raw, context);
                visual.Material = XPointerCodec.ReinterpretPointer<MaterialAsset>(
                    visual.Raw,
                    PointerResolutionKind.Alias);

                context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
                {
                    context.ResolvePointerValue(visual.Material, MaterialWrapperAttribute, visual);
                });
                break;
        }
    }

    private static void TraceFxEffect(string phase, FxEffectDef effect)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx effect {phase}: name=\"{effect.Name}\" elemCount={effect.ElemDefCount} " +
            $"nameRaw=0x{effect.NamePtr?.Raw ?? 0:X8} elemDefsRaw=0x{effect.ElemDefs?.Raw ?? 0:X8}");
    }

    private static void TraceFxElement(string phase, FxElemDef elem)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx elem {phase}: offset=0x{elem.Offset:X} type={(byte)elem.ElemType}({elem.ElemType}) " +
            $"visualCount={elem.VisualCountValue} velCount={elem.VelSampleCount} visCount={elem.VisStateSampleCount} " +
            $"visualRaw=0x{elem.Visuals?.Raw?.Raw ?? 0:X8} extendedRaw=0x{elem.Extended?.Raw ?? 0:X8}");
    }

    private static void TraceFxElement(
        string phase,
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx elem {phase}: offset=0x{elem.Offset:X} type={(byte)elem.ElemType}({elem.ElemType}) " +
            $"velRaw=0x{elem.VelSamples.Raw:X8}/{ReadPatchedCell(elem.VelSamples, context)} " +
            $"visRaw=0x{elem.VisSamples.Raw:X8}/{ReadPatchedCell(elem.VisSamples, context)} " +
            $"visualRaw=0x{elem.Visuals.Raw.Raw:X8}/{ReadPatchedCell(elem.Visuals.Raw, context)} " +
            $"extRaw=0x{elem.Extended.Raw:X8}/{ReadPatchedCell(elem.Extended, context)} " +
            $"block={context.ActiveStreamBlock} pos=0x{context.GetStreamPosition(context.ActiveStreamBlock):X}");
    }

    private static void TraceFxVisualDispatch(FxElemDef elem)
    {
        if (!TraceFxEnabled)
            return;

        string branch = elem.ElemType == FxElemType.Decal
            ? "mark-array"
            : elem.VisualCount > 1
                ? "visual-array"
                : "inline-union";

        Console.Error.WriteLine(
            $"Fx visuals: offset=0x{elem.Offset:X} type={(byte)elem.ElemType}({elem.ElemType}) " +
            $"visualCount={elem.VisualCountValue} branch={branch} raw=0x{elem.Visuals?.Raw?.Raw ?? 0:X8}");
    }

    private static string ReadPatchedCell(
        Pointer pointer,
        IXAssetReaderContext context)
    {
        if (pointer.PatchAddress is not { } patchAddress ||
            !context.TryReadEmittedBytes(patchAddress, 4, out var bytes) ||
            bytes.Length < 4)
        {
            return "????????";
        }

        return $"{BinaryPrimitives.ReadInt32BigEndian(bytes):X8}";
    }

    private static void TraceFxVisualUnion(
        string phase,
        FxElemType elemType,
        int raw,
        IXAssetReaderContext context)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx visual {phase}: type={(byte)elemType}({elemType}) raw=0x{raw:X8} " +
            $"block={context.ActiveStreamBlock} pos=0x{context.GetStreamPosition(context.ActiveStreamBlock):X}");
    }

    private static void Load_FxElemMarkVisuals(
        FxElemMarkVisuals markVisuals,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(markVisuals.Materials0, MaterialWrapperAttribute, markVisuals);
            context.ResolvePointerValue(markVisuals.Materials1, MaterialWrapperAttribute, markVisuals);
        });
    }

    // PS3 0xf5d20 / Xbox Load_FxElemExtendedDefPtr
    private static void Load_FxElemExtendedDefPtr(
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        TraceFxExtended("begin", elem, context);

        if (elem.Extended.IsNull)
        {
            elem.Extended.Value = null;
            TraceFxExtended("null", elem, context);
            return;
        }

        switch (elem.ElemType)
        {
            case FxElemType.Trail:
            {
                var trailPtr = CreatePs3CurrentStreamExtendedPointer<FxTrailDef>(elem.Extended);

                context.ResolvePointerValue(trailPtr, FxTrailDefAttribute, elem);
                elem.Extended.Address = trailPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.Trail,
                    TrailDef = trailPtr.Value
                };
                TraceFxExtended("trail", elem, context);
                break;
            }

            case FxElemType.SparkFountain:
            {
                var sparkPtr = CreatePs3CurrentStreamExtendedPointer<FxSparkFountainDef>(elem.Extended);

                context.ResolvePointerValue(sparkPtr, FxSparkFountainDefAttribute, elem);
                elem.Extended.Address = sparkPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.SparkFountain,
                    SparkFountainDef = sparkPtr.Value
                };
                TraceFxExtended("spark", elem, context);
                break;
            }

            default:
            {
                var unknownPtr = CreatePs3CurrentStreamExtendedPointer<FxElemExtendedUnknown>(elem.Extended);

                context.ResolvePointerValue(unknownPtr, FxExtendedUnknownAttribute, elem);
                elem.Extended.Address = unknownPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.Unknown,
                    UnknownDef = unknownPtr.Value
                };
                TraceFxExtended("unknown", elem, context);
                break;
            }
        }
    }

    private static XPointer<T> CreatePs3CurrentStreamExtendedPointer<T>(Pointer pointer)
    {
        // PS3 0xf5d20 only distinguishes null vs non-null for the Fx extended
        // branches. Any non-zero raw is aligned to the current stream position,
        // the current stream address is patched back into the cell, and the
        // payload is consumed immediately from that current stream.
        int raw = pointer.IsNull ? 0 : -1;

        return XPointerCodec.CreatePointer<T>(
            raw,
            PointerResolutionKind.Direct,
            pointer.PatchAddress);
    }

    private static void ResolveNonNullCurrentStreamPointer<T>(
        XPointer<T> pointer,
        IXAssetReaderContext context,
        XPointerFieldAttribute attribute,
        object owner)
    {
        if (pointer.IsNull)
            return;

        var forcedInline = new XPointer<T>
        {
            Raw = -1,
            Kind = PointerKind.Inline,
            ResolutionKind = pointer.ResolutionKind,
            PatchAddress = pointer.PatchAddress
        };

        context.ResolvePointerValue(forcedInline, attribute, owner);
        pointer.Address = forcedInline.Address;
        pointer.Value = forcedInline.Value;
    }

    // PS3 0xf4ae8 / Xbox Load_FxTrailDef
    private static void Load_FxTrailDef(
        FxTrailDef trailDef,
        IXAssetReaderContext context)
    {
        TraceFxTrail("begin", trailDef, context);
        ResolveNonNullCurrentStreamPointer(trailDef.Verts, context, FxTrailVertsAttribute, trailDef);
        TraceFxTrail("post-verts", trailDef, context);
        ResolveNonNullCurrentStreamPointer(trailDef.Inds, context, FxTrailIndsAttribute, trailDef);
        TraceFxTrail("end", trailDef, context);
    }

    private static void TraceFxExtended(
        string phase,
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx extended {phase}: elemOffset=0x{elem.Offset:X} type={(byte)elem.ElemType}({elem.ElemType}) " +
            $"raw=0x{elem.Extended.Raw:X8} block={context.ActiveStreamBlock} " +
            $"pos=0x{context.GetStreamPosition(context.ActiveStreamBlock):X}");
    }

    private static void TraceFxTrail(
        string phase,
        FxTrailDef trailDef,
        IXAssetReaderContext context)
    {
        if (!TraceFxEnabled)
            return;

        Console.Error.WriteLine(
            $"Fx trail {phase}: verts={trailDef.VertCount} inds={trailDef.IndCount} " +
            $"vertsRaw=0x{trailDef.Verts.Raw:X8} indsRaw=0x{trailDef.Inds.Raw:X8} " +
            $"block={context.ActiveStreamBlock} pos=0x{context.GetStreamPosition(context.ActiveStreamBlock):X}");
    }
}
