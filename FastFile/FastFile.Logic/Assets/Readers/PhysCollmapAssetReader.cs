using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class PhysCollmapAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute CBrushBaseAdjacentSideAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(CBrush.TotalEdgeCount)
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case PhysCollmap collmap:
                Load_PhysCollmap(collmap, context);
                return true;

            case PhysGeomInfo geomInfo:
                Load_PhysGeomInfo(geomInfo, context);
                return true;

            case BrushWrapper brushWrapper:
                Load_BrushWrapper(brushWrapper, context);
                return true;

            case CBrush:
                // CBrush.BaseAdjacentSide needs BrushWrapper.TotalEdgeCount, so
                // BrushWrapper owns the embedded CBrush load sequence.
                return true;

            case CBrushSide brushSide:
                Load_CBrushSide(brushSide, context);
                return true;

            case CPlane:
                return true;

            default:
                return false;
        }
    }

    // PS3 0x106cf0 / Xbox Load_PhysCollmap.
    private static void Load_PhysCollmap(
        PhysCollmap collmap,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(collmap, nameof(PhysCollmap.NamePtr));
            context.ResolvePointerProperty(collmap, nameof(PhysCollmap.Geoms));
        });
    }

    // PS3 0xf7d20 / Xbox Load_PhysGeomInfo.
    private static void Load_PhysGeomInfo(
        PhysGeomInfo geomInfo,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(geomInfo, nameof(PhysGeomInfo.BrushWrapper));
    }

    // PS3 0xf7c68 / Xbox Load_BrushWrapper.
    private static void Load_BrushWrapper(
        BrushWrapper brushWrapper,
        IXAssetReaderContext context)
    {
        Load_CBrush(brushWrapper.Brush, brushWrapper, context);
        context.ResolvePointerProperty(brushWrapper, nameof(BrushWrapper.Planes));
        BackfillBrushSidePlaneValues(brushWrapper);
    }

    // PS3 0xf7ba0 / Xbox embedded cbrush_t load.
    private static void Load_CBrush(
        CBrush brush,
        BrushWrapper brushWrapper,
        IXAssetReaderContext context)
    {
        brush.TotalEdgeCount = brushWrapper.TotalEdgeCount;
        context.ResolvePointerProperty(brush, nameof(CBrush.Sides));
        context.ResolvePointerValue(
            brush.BaseAdjacentSide,
            CBrushBaseAdjacentSideAttribute,
            brush);
    }

    // PS3 0xf76e0 / Xbox Load_CBrushSide.
    private static void Load_CBrushSide(
        CBrushSide brushSide,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(brushSide, nameof(CBrushSide.Plane));
    }

    private static void BackfillBrushSidePlaneValues(BrushWrapper brushWrapper)
    {
        if (brushWrapper.Planes.Value is not { } planes ||
            brushWrapper.Planes.Address is not { } planeArrayAddress ||
            brushWrapper.Brush.Sides.Value is not { } sides)
        {
            return;
        }

        foreach (var side in sides)
        {
            if (side.Plane.Value is not null ||
                side.Plane.Address is not { } planeAddress ||
                planeAddress.Block != planeArrayAddress.Block)
            {
                continue;
            }

            var relativeOffset = planeAddress.Offset - planeArrayAddress.Offset;
            if (relativeOffset < 0 || relativeOffset % CPlane.Size != 0)
                continue;

            var planeIndex = relativeOffset / CPlane.Size;
            if ((uint)planeIndex < (uint)planes.Length)
                side.Plane.Value = planes[planeIndex];
        }
    }
}
