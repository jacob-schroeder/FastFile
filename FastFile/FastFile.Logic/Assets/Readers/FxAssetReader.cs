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
            context.ResolvePointerValue(effect.ElemDefs, FxElemDefArrayAttribute, effect);
        });
    }

    // PS3 0x112e20 / Xbox Load_FxElemDef
    private static void Load_FxElemDef(
        FxElemDef elem,
        IXAssetReaderContext context)
    {
        context.ResolvePointerValue(elem.VelSamples, FxElemVelSamplesAttribute, elem);
        context.ResolvePointerValue(elem.VisSamples, FxElemVisSamplesAttribute, elem);
        Load_FxElemDefVisuals(elem, context);

        Load_FxEffectDefRef(elem.EffectOnImpact, context);
        Load_FxEffectDefRef(elem.EffectOnDeath, context);
        Load_FxEffectDefRef(elem.EffectEmitted, context);

        Load_FxElemExtendedDefPtr(elem, context);
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
        if (elem.ElemType == FxElemType.Decal)
        {
            elem.MarkVisualArray = XPointerCodec.ReinterpretPointer<FxElemMarkVisuals[]>(
                elem.Visuals.Raw,
                PointerResolutionKind.Direct);

            context.ResolvePointerValue(elem.MarkVisualArray, FxElemMarkVisualArrayAttribute, elem);
            return;
        }

        if (elem.VisualCount > 1)
        {
            elem.VisualArray = XPointerCodec.ReinterpretPointer<FxElemDefVisuals[]>(
                elem.Visuals.Raw,
                PointerResolutionKind.Direct);

            context.ResolvePointerValue(elem.VisualArray, FxElemVisualArrayAttribute, elem);

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
        switch (elemType)
        {
            case FxElemType.Model:
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
                break;

            case FxElemType.Sound:
                visual.SoundName = XPointerCodec.ReinterpretPointer<string>(
                    visual.Raw,
                    PointerResolutionKind.Direct);
                context.ResolvePointerValue(visual.SoundName, CStringAttribute, visual);
                break;

            case FxElemType.Runner:
                visual.EffectDef = new FxEffectDefRef
                {
                    NamePtr = XPointerCodec.ReinterpretPointer<string>(
                        visual.Raw,
                        PointerResolutionKind.Direct)
                };
                Load_FxEffectDefRef(visual.EffectDef, context);
                break;

            default:
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
        if (elem.Extended.IsNull)
        {
            elem.Extended.Value = null;
            return;
        }

        switch (elem.ElemType)
        {
            case FxElemType.Trail:
            {
                var trailPtr = XPointerCodec.CreatePointer<FxTrailDef>(
                    elem.Extended.Raw,
                    PointerResolutionKind.Direct,
                    elem.Extended.PatchAddress);

                context.ResolvePointerValue(trailPtr, FxTrailDefAttribute, elem);
                elem.Extended.Address = trailPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.Trail,
                    TrailDef = trailPtr.Value
                };
                break;
            }

            case FxElemType.SparkFountain:
            {
                var sparkPtr = XPointerCodec.CreatePointer<FxSparkFountainDef>(
                    elem.Extended.Raw,
                    PointerResolutionKind.Direct,
                    elem.Extended.PatchAddress);

                context.ResolvePointerValue(sparkPtr, FxSparkFountainDefAttribute, elem);
                elem.Extended.Address = sparkPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.SparkFountain,
                    SparkFountainDef = sparkPtr.Value
                };
                break;
            }

            default:
            {
                var unknownPtr = XPointerCodec.CreatePointer<FxElemExtendedUnknown>(
                    elem.Extended.Raw,
                    PointerResolutionKind.Direct,
                    elem.Extended.PatchAddress);

                context.ResolvePointerValue(unknownPtr, FxExtendedUnknownAttribute, elem);
                elem.Extended.Address = unknownPtr.Address;
                elem.Extended.Value = new FxElemExtendedDef
                {
                    Kind = FxElemExtendedDefKind.Unknown,
                    UnknownDef = unknownPtr.Value
                };
                break;
            }
        }
    }

    // PS3 0xf4ae8 / Xbox Load_FxTrailDef
    private static void Load_FxTrailDef(
        FxTrailDef trailDef,
        IXAssetReaderContext context)
    {
        context.ResolvePointerValue(trailDef.Verts, FxTrailVertsAttribute, trailDef);
        context.ResolvePointerValue(trailDef.Inds, FxTrailIndsAttribute, trailDef);
    }
}
