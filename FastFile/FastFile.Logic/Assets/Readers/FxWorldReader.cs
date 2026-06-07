using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

internal static class FxWorldReader
{
    private const int MaxGlassCount = 1_000_000;
    private const int MaxGlassByteCount = 512 * 1024 * 1024;

    public static FxWorld Read(ref XFileReadContext context)
    {
        var start = context.Position;
        var asset = new FxWorld
        {
            Offset = start,
            NamePtr = context.ReadDirectPointer<string>("FxWorld+0x00.Name"),
            GlassSystem = ReadGlassSystem(ref context),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxWorld.RootSize)
            throw new InvalidDataException($"FxWorld read {bytesRead:N0} bytes; expected {FxWorld.RootSize:N0} bytes.");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ResolveGlassSystem(ref context, asset.GlassSystem);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    private static FxGlassSystem ReadGlassSystem(ref XFileReadContext context)
    {
        var start = context.Position;
        var system = new FxGlassSystem
        {
            Time = context.ReadInt32(),
            PrevTime = context.ReadInt32(),
            DefCount = context.ReadInt32(),
            PieceLimit = context.ReadInt32(),
            PieceWordCount = context.ReadInt32(),
            InitPieceCount = context.ReadInt32(),
            CellCount = context.ReadInt32(),
            ActivePieceCount = context.ReadInt32(),
            FirstFreePiece = context.ReadInt32(),
            GeoDataLimit = context.ReadInt32(),
            GeoDataCount = context.ReadInt32(),
            InitGeoDataCount = context.ReadInt32(),
            Defs = context.ReadDirectPointer<FxGlassDef[]>("FxGlassSystem+0x30.Defs"),
            PiecePlaces = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x34.PiecePlaces"),
            PieceStates = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x38.PieceStates"),
            PieceDynamics = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x3C.PieceDynamics"),
            GeoData = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x40.GeoData"),
            IsInUse = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x44.IsInUse"),
            CellBits = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x48.CellBits"),
            VisData = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x4C.VisData"),
            LinkOrg = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x50.LinkOrg"),
            HalfThickness = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x54.HalfThickness"),
            LightingHandles = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x58.LightingHandles"),
            InitPieceStates = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x5C.InitPieceStates"),
            InitGeoData = context.ReadDirectPointer<byte[]>("FxGlassSystem+0x60.InitGeoData"),
            NeedToCompactData = context.ReadByte(),
            InitCount = context.ReadByte(),
            Padding66To67 = context.ReadBytes(2),
            EffectChanceAccum = context.ReadFloat(),
            LastPieceDeletionTime = context.ReadInt32(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxGlassSystem.RootSize)
            throw new InvalidDataException($"FxGlassSystem read {bytesRead:N0} bytes; expected {FxGlassSystem.RootSize:N0} bytes.");

        return system;
    }

    private static void ResolveGlassSystem(ref XFileReadContext context, FxGlassSystem system)
    {
        ValidateGlassCounts(ref context, system);

        ResolveDefs(ref context, system.Defs, system.DefCount);
        ResolveRawBytesInBlock(
            ref context,
            system.PiecePlaces,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.PieceLimit, FxGlassSystem.PiecePlaceSize, "FxGlassSystem.PiecePlaces"),
            "FxGlassSystem.PiecePlaces");
        ResolveRawBytesInBlock(
            ref context,
            system.PieceStates,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.PieceLimit, FxGlassSystem.PieceStateSize, "FxGlassSystem.PieceStates"),
            "FxGlassSystem.PieceStates");
        ResolveRawBytesInBlock(
            ref context,
            system.PieceDynamics,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.PieceLimit, FxGlassSystem.PieceDynamicsSize, "FxGlassSystem.PieceDynamics"),
            "FxGlassSystem.PieceDynamics");
        ResolveRawBytesInBlock(
            ref context,
            system.GeoData,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.GeoDataLimit, FxGlassSystem.GeometryDataSize, "FxGlassSystem.GeoData"),
            "FxGlassSystem.GeoData");
        ResolveRawBytesInBlock(
            ref context,
            system.IsInUse,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.PieceWordCount, 4, "FxGlassSystem.IsInUse"),
            "FxGlassSystem.IsInUse");
        ResolveRawBytesInBlock(
            ref context,
            system.CellBits,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(CheckedMul(system.PieceWordCount, system.CellCount, "FxGlassSystem.CellBits.Count"), 4, "FxGlassSystem.CellBits"),
            "FxGlassSystem.CellBits");
        ResolveRawBytesInBlock(
            ref context,
            system.VisData,
            XFILE_BLOCK.RUNTIME,
            () => AlignUp(system.PieceLimit, 16),
            "FxGlassSystem.VisData");
        ResolveRawBytesInBlock(
            ref context,
            system.LinkOrg,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(system.PieceLimit, FxGlassSystem.LinkOrgSize, "FxGlassSystem.LinkOrg"),
            "FxGlassSystem.LinkOrg");
        ResolveRawBytesInBlock(
            ref context,
            system.HalfThickness,
            XFILE_BLOCK.RUNTIME,
            () => CheckedMul(AlignUp(system.PieceLimit, 4), 4, "FxGlassSystem.HalfThickness"),
            "FxGlassSystem.HalfThickness");
        ResolveRawBytesInBlock(
            ref context,
            system.LightingHandles,
            XFILE_BLOCK.LARGE,
            () => CheckedMul(system.InitPieceCount, 2, "FxGlassSystem.LightingHandles"),
            "FxGlassSystem.LightingHandles");
        ResolveRawBytesInBlock(
            ref context,
            system.InitPieceStates,
            XFILE_BLOCK.LARGE,
            () => CheckedMul(system.InitPieceCount, FxGlassSystem.InitPieceStateSize, "FxGlassSystem.InitPieceStates"),
            "FxGlassSystem.InitPieceStates");
        ResolveRawBytesInBlock(
            ref context,
            system.InitGeoData,
            XFILE_BLOCK.LARGE,
            () => CheckedMul(system.InitGeoDataCount, FxGlassSystem.GeometryDataSize, "FxGlassSystem.InitGeoData"),
            "FxGlassSystem.InitGeoData");
    }

    private static void ValidateGlassCounts(ref XFileReadContext context, FxGlassSystem system)
    {
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.DefCount", system.DefCount, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x08 as FxGlassDef count.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.PieceLimit", system.PieceLimit, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x0C for runtime glass piece arrays.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.PieceWordCount", system.PieceWordCount, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x10 for isInUse/cellBits word counts.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.InitPieceCount", system.InitPieceCount, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x14 for lightingHandles/initPieceStates.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.CellCount", system.CellCount, 0, MaxGlassCount, "EBOOT 0x0010faa0 multiplies +0x10 by +0x18 for cellBits.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.GeoDataLimit", system.GeoDataLimit, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x24 for geoData.");
        XFileReadValidator.ValidateCount(ref context, "FxGlassSystem.InitGeoDataCount", system.InitGeoDataCount, 0, MaxGlassCount, "EBOOT 0x0010faa0 uses +0x2C for initGeoData.");
    }

    private static void ResolveDefs(
        ref XFileReadContext context,
        ZonePointer<FxGlassDef[]> pointer,
        int count)
    {
        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxGlassDef[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var defs = new FxGlassDef[count];
                    for (var i = 0; i < defs.Length; i++)
                        defs[i] = ReadFxGlassDef(ref valueContext, i);

                    foreach (var def in defs)
                    {
                        PhysicsReader.ResolvePhysPresetPointerNow(ref valueContext, def.PhysPreset);
                        MaterialReader.ResolveMaterialPointerNow(ref valueContext, def.Material);
                        MaterialReader.ResolveMaterialPointerNow(ref valueContext, def.MaterialShattered);
                    }

                    return defs;
                }));
        });
    }

    private static FxGlassDef ReadFxGlassDef(ref XFileReadContext context, int index)
    {
        var start = context.Position;
        var def = new FxGlassDef
        {
            HalfThickness = context.ReadFloat(),
            TexVecs = ReadFloatArray(ref context, 4),
            Color = context.ReadInt32(),
            Material = context.ReadAliasPointer<MaterialAsset>($"FxGlassDef[{index}]+0x18.Material"),
            MaterialShattered = context.ReadAliasPointer<MaterialAsset>($"FxGlassDef[{index}]+0x1C.MaterialShattered"),
            PhysPreset = context.ReadAliasPointer<FastFile.Models.Assets.Physics.PhysPreset>($"FxGlassDef[{index}]+0x20.PhysPreset"),
            InvHighMipRadius = context.ReadFloat(),
            ShatteredInvHighMipRadius = context.ReadFloat(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != FxGlassSystem.FxGlassDefSize)
            throw new InvalidDataException($"FxGlassDef read {bytesRead:N0} bytes; expected {FxGlassSystem.FxGlassDefSize:N0} bytes.");

        return def;
    }

    private static void ResolveRawBytesInBlock(
        ref XFileReadContext context,
        ZonePointer<byte[]> pointer,
        XFILE_BLOCK block,
        Func<int> byteCountFactory,
        string fieldPath)
    {
        context.ResolvePointerNowInBlock(
            pointer,
            block,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var byteCount = byteCountFactory();
                        XFileReadValidator.ValidateCount(
                            ref valueContext,
                            fieldPath,
                            byteCount,
                            0,
                            MaxGlassByteCount,
                            "EBOOT 0x0010faa0 selects this payload size from FxGlassSystem count fields before calling Load_Stream.");

                        return byteCount == 0 ? [] : valueContext.ReadBytes(byteCount);
                    }));
            });
    }

    private static float[] ReadFloatArray(ref XFileReadContext context, int count)
    {
        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();

        return values;
    }

    private static int AlignUp(int value, int alignment)
    {
        if (value < 0)
            throw new InvalidDataException($"Cannot align negative FxGlassSystem count {value:N0}.");

        return checked((value + alignment - 1) & ~(alignment - 1));
    }

    private static int CheckedMul(int count, int stride, string fieldPath)
    {
        if (count < 0 || stride < 0)
            throw new InvalidDataException($"{fieldPath} has a negative count/stride ({count:N0} * {stride:N0}).");

        try
        {
            return checked(count * stride);
        }
        catch (OverflowException ex)
        {
            throw new InvalidDataException($"{fieldPath} byte count overflowed ({count:N0} * {stride:N0}).", ex);
        }
    }
}
