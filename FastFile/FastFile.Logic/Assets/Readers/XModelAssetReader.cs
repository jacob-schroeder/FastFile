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
        TraceXModel(
            context,
            $"Load_XModel begin nameRaw=0x{model.NamePtr.Raw:X8} numSurfs={model.NumSurfs} numLods={model.NumLods}");

        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(model, nameof(XModel.NamePtr));
            TraceXModel(context, $"Load_XModel name=\"{model.Name}\"");

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
                TraceXModel(
                    context,
                    $"Load_XModel phys begin physPresetRaw=0x{model.PhysPreset.Raw:X8} physCollmapRaw=0x{model.PhysCollmap.Raw:X8}");
                context.ResolvePointerValue(model.PhysPreset, PhysWrapperAttribute, model);
                TraceXModel(context, "Load_XModel phys preset done");
                context.ResolvePointerValue(model.PhysCollmap, PhysWrapperAttribute, model);
                TraceXModel(context, "Load_XModel phys collmap done");
            });
        });

        TraceXModel(context, "Load_XModel end");
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
            TraceXModel(context, "Load_XModelLodInfo modelSurfs begin");
            context.ResolvePointerValue(lodInfo.ModelSurfs, XModelSurfsWrapperAttribute, lodInfo);
            TraceXModel(
                context,
                $"Load_XModelLodInfo modelSurfs end name=\"{lodInfo.ModelSurfs.Value?.Name}\"");
        });
    }

    // PS3 0x1067e0 / Xbox Load_XModelSurfs
    private static void Load_XModelSurfs(
        XModelSurfs modelSurfs,
        IXAssetReaderContext context)
    {
        TraceXModel(
            context,
            $"Load_XModelSurfs begin nameRaw=0x{modelSurfs.NamePtr.Raw:X8} surfsRaw=0x{modelSurfs.Surfs.Raw:X8} numSurfs={modelSurfs.NumSurfs}");

        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(modelSurfs, nameof(XModelSurfs.NamePtr));
            TraceXModel(context, $"Load_XModelSurfs name=\"{modelSurfs.Name}\"");
            context.ResolvePointerProperty(modelSurfs, nameof(XModelSurfs.Surfs));
            TraceXModel(context, "Load_XModelSurfs surfs done");
        });

        TraceXModel(context, "Load_XModelSurfs end");
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

    private static void TraceXModel(
        IXAssetReaderContext context,
        string message)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_XMODEL") != "1")
            return;

        Console.Error.WriteLine(
            $"XModelTrace: src=0x{context.SourcePosition:X} active={context.ActiveStreamBlock} " +
            $"temp=0x{context.GetStreamPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{context.GetStreamPosition(XFILE_BLOCK.LARGE):X} {message}");
    }
}
