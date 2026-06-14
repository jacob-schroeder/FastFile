using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

public sealed class XModelAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute MaterialWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute XModelSurfsWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute PhysWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case XModel model:
                Load_XModel(model, context);
                return true;

            case XModelSurfs modelSurfs:
                Load_XModelSurfs(modelSurfs, context);
                return true;

            case XModelLodInfo lodInfo:
                Load_XModelLodInfo(lodInfo, context);
                return true;

            default:
                return false;
        }
    }

    // PS3 0x111698 / Xbox Load_XModel
    private static void Load_XModel(
        XModel model,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(model, nameof(XModel.NamePtr));

            context.ResolvePointerProperty(model, nameof(XModel.BoneNames));
            context.ResolvePointerProperty(model, nameof(XModel.ParentList));
            context.ResolvePointerProperty(model, nameof(XModel.Quats));
            context.ResolvePointerProperty(model, nameof(XModel.Trans));
            context.ResolvePointerProperty(model, nameof(XModel.PartClassification));
            context.ResolvePointerProperty(model, nameof(XModel.BaseMat));

            Load_MaterialHandleArray(model, context);

            foreach (var lodInfo in model.LodInfo)
                Load_XModelLodInfo(lodInfo, context);

            context.ResolvePointerProperty(model, nameof(XModel.CollSurfs));
            context.ResolvePointerProperty(model, nameof(XModel.BoneInfo));
            context.ResolvePointerProperty(model, nameof(XModel.InvHighMipRadius));

            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.ResolvePointerValue(model.PhysPreset, PhysWrapperAttribute, model);
                context.ResolvePointerValue(model.PhysCollmap, PhysWrapperAttribute, model);
            });
        });
    }

    // PS3 0x106990 / Xbox Load_XModelLodInfo
    private static void Load_XModelLodInfo(
        XModelLodInfo lodInfo,
        IXAssetReaderContext context)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_XMODEL") == "1")
        {
            Console.Error.WriteLine(
                $"XModelLodInfo: dist={lodInfo.Dist} numSurfs={lodInfo.NumSurfs} surfIndex={lodInfo.SurfIndex} " +
                $"modelSurfsRaw=0x{lodInfo.ModelSurfs.Raw:X8} kind={lodInfo.ModelSurfs.Kind}");
        }

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(lodInfo.ModelSurfs, XModelSurfsWrapperAttribute, lodInfo);
        });
    }

    // PS3 0x1067e0 / Xbox Load_XModelSurfs
    private static void Load_XModelSurfs(
        XModelSurfs modelSurfs,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(modelSurfs, nameof(XModelSurfs.NamePtr));
            context.ResolvePointerProperty(modelSurfs, nameof(XModelSurfs.Surfs));
        });
    }

    // PS3 0x1104a0 loops Load_MaterialPtr for each pointer cell.
    private static void Load_MaterialHandleArray(
        XModel model,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(model, nameof(XModel.MaterialHandles));

        if (model.MaterialHandles.Value is not { } materialHandles)
            return;

        foreach (XPointer<MaterialAsset>? material in materialHandles)
        {
            if (material is not { IsResolved: false })
                continue;

            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.ResolvePointerValue(material, MaterialWrapperAttribute, model);
            });
        }
    }
}
