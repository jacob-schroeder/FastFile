using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class PhysicsReader
{
    private const int PhysGeomInfoSize = 0x44;
    private const int BrushWrapperSize = 0x44;
    private const int CBrushSize = 0x24;
    private const int CBrushSideSize = 0x08;
    private const int CPlaneSize = 0x14;

    public static PhysPreset ReadPhysPreset(ref XFileReadContext context)
    {
        var asset = new PhysPreset
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            PresetType = context.ReadInt32(),
            Mass = context.ReadFloat(),
            Bounce = context.ReadFloat(),
            Friction = context.ReadFloat(),
            BulletForceScale = context.ReadFloat(),
            ExplosiveForceScale = context.ReadFloat(),
            SndAliasPrefix = GenericReader.ReadStringPointer(ref context, resolve: false),
            PiecesSpreadFraction = context.ReadFloat(),
            PiecesUpwardVelocity = context.ReadFloat(),
            TempDefaultToCylinder = context.ReadByte() != 0,
            PerSurfaceSndAlias = context.ReadByte() != 0,
            BoolAlignmentPadding = context.ReadUInt16(),
        };

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            GenericReader.ResolveStringPointerNow(ref context, asset.SndAliasPrefix);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    public static PhysCollmap ReadPhysCollmap(ref XFileReadContext context)
    {
        var asset = new PhysCollmap
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            Count = context.ReadUInt32(),
            Geoms = context.ReadDirectPointer<PhysGeomInfo[]>("PhysCollmap.Geoms"),
            Mass = ReadPhysMass(ref context),
            Bounds = ReadBounds(ref context),
        };

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ResolvePhysGeomInfoArray(ref context, asset.Geoms, asset.Count);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    public static ZonePointer<PhysPreset> ReadPhysPresetPointer(ref XFileReadContext context)
    {
        var pointer = ReadPhysPresetPointerField(ref context);
        ResolvePhysPresetPointer(ref context, pointer);
        return pointer;
    }

    public static AliasPointer<PhysPreset> ReadPhysPresetPointerField(
        ref XFileReadContext context,
        string fieldPath = "PhysPresetAssetRef")
    {
        return context.ReadAliasPointer<PhysPreset>(fieldPath);
    }

    public static void ResolvePhysPresetPointer(
        ref XFileReadContext context,
        ZonePointer<PhysPreset> pointer)
    {
        context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadPhysPresetPointerValue);
    }

    public static void ResolvePhysPresetPointerNow(
        ref XFileReadContext context,
        ZonePointer<PhysPreset> pointer)
    {
        context.ResolvePointerNowInBlock(pointer, XFILE_BLOCK.TEMP, ReadPhysPresetPointerValue);
    }

    public static ZonePointer<PhysCollmap> ReadPhysCollmapPointer(ref XFileReadContext context)
    {
        var pointer = ReadPhysCollmapPointerField(ref context);
        ResolvePhysCollmapPointer(ref context, pointer);
        return pointer;
    }

    public static AliasPointer<PhysCollmap> ReadPhysCollmapPointerField(
        ref XFileReadContext context,
        string fieldPath = "PhysCollmapAssetRef")
    {
        return context.ReadAliasPointer<PhysCollmap>(fieldPath);
    }

    public static void ResolvePhysCollmapPointer(
        ref XFileReadContext context,
        ZonePointer<PhysCollmap> pointer)
    {
        context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadPhysCollmapPointerValue);
    }

    public static void ResolvePhysCollmapPointerNow(
        ref XFileReadContext context,
        ZonePointer<PhysCollmap> pointer)
    {
        context.ResolvePointerNowInBlock(pointer, XFILE_BLOCK.TEMP, ReadPhysCollmapPointerValue);
    }

    private static void ReadPhysPresetPointerValue(
        ref XFileReadContext context,
        ZonePointer<PhysPreset> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadPhysPreset));
    }

    private static void ReadPhysCollmapPointerValue(
        ref XFileReadContext context,
        ZonePointer<PhysCollmap> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadPhysCollmap));
    }

    private static void ResolvePhysGeomInfoArray(
        ref XFileReadContext context,
        ZonePointer<PhysGeomInfo[]> pointer,
        uint count)
    {
        if (count == 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<PhysGeomInfo[]> p) =>
            {
                var values = new PhysGeomInfo[checked((int)count)];
                for (var i = 0; i < values.Length; i++)
                {
                    var start = valueContext.Position;
                    values[i] = ReadPhysGeomInfo(ref valueContext);
                    var bytesRead = valueContext.Position - start;
                    if (bytesRead != PhysGeomInfoSize)
                    {
                        throw new InvalidDataException(
                            $"PhysGeomInfo read {bytesRead:N0} bytes; expected {PhysGeomInfoSize:N0} bytes.");
                    }
                }

                p.SetResult(values);

                foreach (var value in values)
                    ResolveBrushWrapperPointer(ref valueContext, value.BrushWrapper);
            });
    }

    private static PhysGeomInfo ReadPhysGeomInfo(ref XFileReadContext context)
    {
        return new PhysGeomInfo
        {
            BrushWrapper = context.ReadDirectPointer<BrushWrapper>("PhysGeomInfo.BrushWrapper"),
            Type = context.ReadInt32(),
            Orientation =
            [
                ReadVec3(ref context),
                ReadVec3(ref context),
                ReadVec3(ref context),
            ],
            Bounds = ReadBounds(ref context),
        };
    }

    private static void ResolveBrushWrapperPointer(
        ref XFileReadContext context,
        ZonePointer<BrushWrapper> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<BrushWrapper> p) =>
            {
                var start = valueContext.Position;
                var wrapper = ReadBrushWrapper(ref valueContext);
                var bytesRead = valueContext.Position - start;
                if (bytesRead != BrushWrapperSize)
                {
                    throw new InvalidDataException(
                        $"BrushWrapper read {bytesRead:N0} bytes; expected {BrushWrapperSize:N0} bytes.");
                }

                p.SetResult(wrapper);
                ResolveCBrushChildren(ref valueContext, wrapper.Brush, wrapper.TotalEdgeCount);
                ResolveCPlaneArray(ref valueContext, wrapper.Planes, wrapper.Brush.NumSides);
            });
    }

    private static BrushWrapper ReadBrushWrapper(ref XFileReadContext context)
    {
        return new BrushWrapper
        {
            Bounds = ReadBounds(ref context),
            Brush = ReadCBrush(ref context),
            TotalEdgeCount = context.ReadInt32(),
            Planes = context.ReadDirectPointer<CPlane[]>("BrushWrapper.Planes"),
        };
    }

    private static CBrush ReadCBrush(ref XFileReadContext context)
    {
        var start = context.Position;
        var brush = new CBrush
        {
            NumSides = context.ReadUInt16(),
            GlassPieceIndex = context.ReadUInt16(),
            Sides = context.ReadDirectPointer<CBrushSide[]>("CBrush.Sides"),
            BaseAdjacentSide = context.ReadDirectPointer<byte[]>("CBrush.BaseAdjacentSide"),
        };

        for (var i = 0; i < brush.AxialMaterialNum.Length; i++)
            brush.AxialMaterialNum[i] = unchecked((short)context.ReadUInt16());

        brush.FirstAdjacentSideOffsets = context.ReadBytes(brush.FirstAdjacentSideOffsets.Length);
        brush.EdgeCount = context.ReadBytes(brush.EdgeCount.Length);

        var bytesRead = context.Position - start;
        if (bytesRead != CBrushSize)
            throw new InvalidDataException($"CBrush read {bytesRead:N0} bytes; expected {CBrushSize:N0} bytes.");

        return brush;
    }

    private static void ResolveCBrushChildren(
        ref XFileReadContext context,
        CBrush brush,
        int totalEdgeCount)
    {
        ResolveCBrushSideArray(ref context, brush.Sides, brush.NumSides);
        ResolveByteArray(ref context, brush.BaseAdjacentSide, totalEdgeCount);
    }

    private static void ResolveCBrushSideArray(
        ref XFileReadContext context,
        ZonePointer<CBrushSide[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<CBrushSide[]> p) =>
            {
                var values = new CBrushSide[count];
                for (var i = 0; i < values.Length; i++)
                {
                    var start = valueContext.Position;
                    values[i] = ReadCBrushSide(ref valueContext);
                    var bytesRead = valueContext.Position - start;
                    if (bytesRead != CBrushSideSize)
                    {
                        throw new InvalidDataException(
                            $"CBrushSide read {bytesRead:N0} bytes; expected {CBrushSideSize:N0} bytes.");
                    }
                }

                p.SetResult(values);

                foreach (var value in values)
                    ResolveCPlanePointer(ref valueContext, value.Plane);
            });
    }

    private static CBrushSide ReadCBrushSide(ref XFileReadContext context)
    {
        return new CBrushSide
        {
            Plane = context.ReadDirectPointer<CPlane>("CBrushSide.Plane"),
            MaterialNum = context.ReadUInt16(),
            FirstAdjacentSideOffset = context.ReadByte(),
            EdgeCount = context.ReadByte(),
        };
    }

    private static void ResolveCPlanePointer(
        ref XFileReadContext context,
        ZonePointer<CPlane> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<CPlane> p) =>
            {
                p.SetResult(valueContext.ReadPointerValue(p, ReadCPlane));
            });
    }

    private static void ResolveCPlaneArray(
        ref XFileReadContext context,
        ZonePointer<CPlane[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<CPlane[]> p) =>
            {
                var values = new CPlane[count];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadCPlane(ref valueContext);

                p.SetResult(values);
            });
    }

    private static CPlane ReadCPlane(ref XFileReadContext context)
    {
        var start = context.Position;
        var plane = new CPlane
        {
            Normal = ReadVec3(ref context),
            Dist = context.ReadFloat(),
            Type = context.ReadByte(),
            Padding = context.ReadBytes(3),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != CPlaneSize)
            throw new InvalidDataException($"CPlane read {bytesRead:N0} bytes; expected {CPlaneSize:N0} bytes.");

        return plane;
    }

    private static void ResolveByteArray(
        ref XFileReadContext context,
        ZonePointer<byte[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext valueContext, ZonePointer<byte[]> p) =>
            {
                p.SetResult(valueContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext byteContext) => byteContext.ReadBytes(count)));
            });
    }

    private static PhysMass ReadPhysMass(ref XFileReadContext context)
    {
        return new PhysMass
        {
            CenterOfMass = ReadVec3(ref context),
            MomentsOfInertia = ReadVec3(ref context),
            ProductsOfInertia = ReadVec3(ref context),
        };
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
