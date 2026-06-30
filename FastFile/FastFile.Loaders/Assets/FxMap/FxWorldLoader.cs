using FastFile.Loaders.Assets.Material;
using FastFile.Models.Assets.FxMap;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Vehicle;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.FxMap;

public sealed class FxWorldLoader
{
    private readonly MaterialLoader _materialLoader = new();

    public FxWorldAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level FxWorld pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            FxWorldAsset fxWorld = ReadFxWorld(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return fxWorld;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private FxWorldAsset ReadFxWorld(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, FxWorldAsset.SerializedSize, out XBlockAddress rootAddress, "FxWorld");
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"FxWorld pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        FxGlassSystem glassSystem = ReadFxGlassSystemHeader(rootCursor, context);

        if (rootCursor.Offset != FxWorldAsset.SerializedSize)
            throw new InvalidDataException($"FxWorld consumed 0x{rootCursor.Offset:X} bytes instead of 0x{FxWorldAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"  FxWorld.header source=0x{sourceOffset:X}..0x{sourceOffset + FxWorldAsset.SerializedSize:X} " +
            $"root={rootAddress} name=0x{namePointer.Raw:X8}/{namePointer.Untyped.Type} bytes={PreviewBytes(rootBytes, 48)}");
        TraceFxGlassHeader(context, sourceOffset + 4, glassSystem);

        string? name;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            glassSystem = ReadFxGlassSystemPayloads(cursor, glassSystem, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  FxWorld root source=0x{sourceOffset:X} name={name ?? "<null>"} " +
            $"defs={glassSystem.Defs.Count}/{glassSystem.DefCount} pieces={glassSystem.PieceStates.Count}/{glassSystem.PieceLimit} " +
            $"initPieces={glassSystem.InitPieceStates.Count}/{glassSystem.InitPieceCount} blocks={context.Blocks.DescribePositions()}");

        return new FxWorldAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            GlassSystem = glassSystem
        };
    }

    private static FxGlassSystem ReadFxGlassSystemHeader(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int start = cursor.Offset;
        XBlockAddress address = cursor.AddressAt(start)
            ?? throw new InvalidDataException("FxGlassSystem cursor has no destination block address.");
        int time = cursor.ReadInt32();
        int prevTime = cursor.ReadInt32();
        uint defCount = cursor.ReadUInt32();
        uint pieceLimit = cursor.ReadUInt32();
        uint pieceWordCount = cursor.ReadUInt32();
        uint initPieceCount = cursor.ReadUInt32();
        uint cellCount = cursor.ReadUInt32();
        uint activePieceCount = cursor.ReadUInt32();
        uint firstFreePiece = cursor.ReadUInt32();
        uint geoDataLimit = cursor.ReadUInt32();
        uint geoDataCount = cursor.ReadUInt32();
        uint initGeoDataCount = cursor.ReadUInt32();
        XPointer<FxGlassDef[]> defsPointer = context.PointerReader.ReadPointer<FxGlassDef[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassPiecePlace[]> piecePlacesPointer = context.PointerReader.ReadPointer<FxGlassPiecePlace[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassPieceState[]> pieceStatesPointer = context.PointerReader.ReadPointer<FxGlassPieceState[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassPieceDynamics[]> pieceDynamicsPointer = context.PointerReader.ReadPointer<FxGlassPieceDynamics[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassGeometryData[]> geoDataPointer = context.PointerReader.ReadPointer<FxGlassGeometryData[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<uint[]> isInUsePointer = context.PointerReader.ReadPointer<uint[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<uint[]> cellBitsPointer = context.PointerReader.ReadPointer<uint[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<byte[]> visDataPointer = context.PointerReader.ReadPointer<byte[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxVec3[]> linkOrgPointer = context.PointerReader.ReadPointer<FxVec3[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<float[]> halfThicknessPointer = context.PointerReader.ReadPointer<float[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<ushort[]> lightingHandlesPointer = context.PointerReader.ReadPointer<ushort[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassInitPieceState[]> initPieceStatesPointer = context.PointerReader.ReadPointer<FxGlassInitPieceState[]>(cursor, XPointerResolutionMode.Direct);
        XPointer<FxGlassGeometryData[]> initGeoDataPointer = context.PointerReader.ReadPointer<FxGlassGeometryData[]>(cursor, XPointerResolutionMode.Direct);
        byte needToCompactData = cursor.ReadByte();
        byte initCount = cursor.ReadByte();
        ushort pad66 = cursor.ReadUInt16();
        float effectChanceAccum = ReadSingle(cursor);
        int lastPieceDeletionTime = cursor.ReadInt32();

        if (cursor.Offset - start != FxGlassSystem.SerializedSize)
            throw new InvalidDataException($"FxGlassSystem consumed 0x{cursor.Offset - start:X} bytes instead of 0x{FxGlassSystem.SerializedSize:X}.");

        return new FxGlassSystem
        {
            Offset = address.Offset,
            Time = time,
            PrevTime = prevTime,
            DefCount = defCount,
            PieceLimit = pieceLimit,
            PieceWordCount = pieceWordCount,
            InitPieceCount = initPieceCount,
            CellCount = cellCount,
            ActivePieceCount = activePieceCount,
            FirstFreePiece = firstFreePiece,
            GeoDataLimit = geoDataLimit,
            GeoDataCount = geoDataCount,
            InitGeoDataCount = initGeoDataCount,
            DefsPointer = defsPointer,
            PiecePlacesPointer = piecePlacesPointer,
            PieceStatesPointer = pieceStatesPointer,
            PieceDynamicsPointer = pieceDynamicsPointer,
            GeoDataPointer = geoDataPointer,
            IsInUsePointer = isInUsePointer,
            CellBitsPointer = cellBitsPointer,
            VisDataPointer = visDataPointer,
            LinkOrgPointer = linkOrgPointer,
            HalfThicknessPointer = halfThicknessPointer,
            LightingHandlesPointer = lightingHandlesPointer,
            InitPieceStatesPointer = initPieceStatesPointer,
            InitGeoDataPointer = initGeoDataPointer,
            NeedToCompactData = needToCompactData,
            InitCount = initCount,
            Pad66 = pad66,
            EffectChanceAccum = effectChanceAccum,
            LastPieceDeletionTime = lastPieceDeletionTime
        };
    }

    private FxGlassSystem ReadFxGlassSystemPayloads(
        FastFileCursor cursor,
        FxGlassSystem header,
        FastFileLoadContext context)
    {
        context.Diagnostics.Trace(
            $"    FxGlassSystem.payloads.begin source=0x{cursor.Offset:X} " +
            $"defs={header.DefCount} pieceLimit={header.PieceLimit} pieceWordCount={header.PieceWordCount} " +
            $"cellCount={header.CellCount} initPieceCount={header.InitPieceCount} initGeoDataCount={header.InitGeoDataCount}");

        IReadOnlyList<FxGlassDef> defs = ReadFxGlassDefs(cursor, header.DefsPointer.Untyped, Count(header.DefCount, "defCount"), context);

        IReadOnlyList<FxGlassPiecePlace> piecePlaces;
        IReadOnlyList<FxGlassPieceState> pieceStates;
        IReadOnlyList<FxGlassPieceDynamics> pieceDynamics;
        IReadOnlyList<FxGlassGeometryData> geoData;
        IReadOnlyList<uint> isInUse;
        IReadOnlyList<uint> cellBits;
        IReadOnlyList<byte> visData;
        IReadOnlyList<FxVec3> linkOrg;
        IReadOnlyList<float> halfThickness;

        int pieceLimit = Count(header.PieceLimit, "pieceLimit");
        int pieceWordCount = Count(header.PieceWordCount, "pieceWordCount");
        int cellCount = Count(header.CellCount, "cellCount");
        piecePlaces = ReadPushedRuntime(context, () => ReadPiecePlaces(cursor, header.PiecePlacesPointer.Untyped, pieceLimit, context));
        pieceStates = ReadPushedRuntime(context, () => ReadPieceStates(cursor, header.PieceStatesPointer.Untyped, pieceLimit, context));
        pieceDynamics = ReadPushedRuntime(context, () => ReadPieceDynamics(cursor, header.PieceDynamicsPointer.Untyped, pieceLimit, context));
        geoData = ReadPushedRuntime(context, () => ReadGeometryData(cursor, header.GeoDataPointer.Untyped, Count(header.GeoDataLimit, "geoDataLimit"), context, "FxGlassSystem.geoData"));
        isInUse = ReadPushedRuntime(context, () => ReadUInt32Array(cursor, header.IsInUsePointer.Untyped, pieceWordCount, 4, context, "FxGlassSystem.isInUse"));
        cellBits = ReadPushedRuntime(context, () => ReadUInt32Array(cursor, header.CellBitsPointer.Untyped, checked(cellCount * pieceWordCount), 4, context, "FxGlassSystem.cellBits"));
        visData = ReadPushedRuntime(context, () => ReadByteArray(cursor, header.VisDataPointer.Untyped, Align(pieceLimit, 16), 16, context, "FxGlassSystem.visData"));
        linkOrg = ReadPushedRuntime(context, () => ReadVec3Array(cursor, header.LinkOrgPointer.Untyped, pieceLimit, context));
        halfThickness = ReadPushedRuntime(context, () => ReadFloatArray(cursor, header.HalfThicknessPointer.Untyped, Align(pieceLimit, 4), 16, context, "FxGlassSystem.halfThickness"));

        IReadOnlyList<ushort> lightingHandles = ReadUInt16Array(
            cursor,
            header.LightingHandlesPointer.Untyped,
            Count(header.InitPieceCount, "initPieceCount"),
            2,
            context,
            "FxGlassSystem.lightingHandles");
        IReadOnlyList<FxGlassInitPieceState> initPieceStates = ReadInitPieceStates(
            cursor,
            header.InitPieceStatesPointer.Untyped,
            Count(header.InitPieceCount, "initPieceCount"),
            context);
        IReadOnlyList<FxGlassGeometryData> initGeoData = ReadGeometryData(
            cursor,
            header.InitGeoDataPointer.Untyped,
            Count(header.InitGeoDataCount, "initGeoDataCount"),
            context,
            "FxGlassSystem.initGeoData");

        context.Diagnostics.Trace(
            $"    FxGlassSystem.payloads.end source=0x{cursor.Offset:X} " +
            $"loaded defs={defs.Count} piecePlaces={piecePlaces.Count} pieceStates={pieceStates.Count} " +
            $"pieceDynamics={pieceDynamics.Count} geoData={geoData.Count} lightingHandles={lightingHandles.Count} " +
            $"initPieceStates={initPieceStates.Count} initGeoData={initGeoData.Count} blocks={context.Blocks.DescribePositions()}");

        return new FxGlassSystem
        {
            Offset = header.Offset,
            Time = header.Time,
            PrevTime = header.PrevTime,
            DefCount = header.DefCount,
            PieceLimit = header.PieceLimit,
            PieceWordCount = header.PieceWordCount,
            InitPieceCount = header.InitPieceCount,
            CellCount = header.CellCount,
            ActivePieceCount = header.ActivePieceCount,
            FirstFreePiece = header.FirstFreePiece,
            GeoDataLimit = header.GeoDataLimit,
            GeoDataCount = header.GeoDataCount,
            InitGeoDataCount = header.InitGeoDataCount,
            DefsPointer = header.DefsPointer,
            Defs = defs,
            PiecePlacesPointer = header.PiecePlacesPointer,
            PiecePlaces = piecePlaces,
            PieceStatesPointer = header.PieceStatesPointer,
            PieceStates = pieceStates,
            PieceDynamicsPointer = header.PieceDynamicsPointer,
            PieceDynamics = pieceDynamics,
            GeoDataPointer = header.GeoDataPointer,
            GeoData = geoData,
            IsInUsePointer = header.IsInUsePointer,
            IsInUse = isInUse,
            CellBitsPointer = header.CellBitsPointer,
            CellBits = cellBits,
            VisDataPointer = header.VisDataPointer,
            VisData = visData,
            LinkOrgPointer = header.LinkOrgPointer,
            LinkOrg = linkOrg,
            HalfThicknessPointer = header.HalfThicknessPointer,
            HalfThickness = halfThickness,
            LightingHandlesPointer = header.LightingHandlesPointer,
            LightingHandles = lightingHandles,
            InitPieceStatesPointer = header.InitPieceStatesPointer,
            InitPieceStates = initPieceStates,
            InitGeoDataPointer = header.InitGeoDataPointer,
            InitGeoData = initGeoData,
            NeedToCompactData = header.NeedToCompactData,
            InitCount = header.InitCount,
            Pad66 = header.Pad66,
            EffectChanceAccum = header.EffectChanceAccum,
            LastPieceDeletionTime = header.LastPieceDeletionTime
        };
    }

    private IReadOnlyList<FxGlassDef> ReadFxGlassDefs(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        XBlockAddress defsAddress = PatchRequiredInlineArray(pointer, count, context, "FxGlassSystem.defs");
        int sourceStart = cursor.Offset;
        int byteCount = checked(count * FxGlassDef.SerializedSize);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress loadedAddress, "FxGlassSystem.defs");
        if (loadedAddress != defsAddress)
            throw new InvalidDataException($"FxGlassSystem.defs pointer patched to {defsAddress}, but array loaded at {loadedAddress}.");

        TraceArrayLoad(
            context,
            "FxGlassSystem.defs",
            pointer,
            count,
            FxGlassDef.SerializedSize,
            4,
            sourceStart,
            cursor.Offset,
            byteCount,
            defsAddress,
            bytes);

        var rowCursor = new FastFileCursor(bytes, defsAddress);
        var defs = new FxGlassDef[count];
        for (int i = 0; i < defs.Length; i++)
            defs[i] = ReadFxGlassDef(cursor, rowCursor, context, i);

        return defs;
    }

    private static T ReadPushedRuntime<T>(
        FastFileLoadContext context,
        Func<T> read)
    {
        context.Blocks.Push(XFileBlockType.RUNTIME);
        try
        {
            return read();
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private FxGlassDef ReadFxGlassDef(
        FastFileCursor cursor,
        FastFileCursor rowCursor,
        FastFileLoadContext context,
        int index)
    {
        int start = rowCursor.Offset;
        XBlockAddress rowAddress = rowCursor.AddressAt(start)
            ?? throw new InvalidDataException("FxGlassDef cursor has no destination block address.");
        float halfThickness = ReadSingle(rowCursor);
        FxVec2 texVec0 = ReadVec2(rowCursor);
        FxVec2 texVec1 = ReadVec2(rowCursor);
        uint color = rowCursor.ReadUInt32();
        XPointer<MaterialAsset> materialPointer = context.PointerReader.ReadPointer<MaterialAsset>(rowCursor, XPointerResolutionMode.AliasCell);
        XPointer<MaterialAsset> materialShatteredPointer = context.PointerReader.ReadPointer<MaterialAsset>(rowCursor, XPointerResolutionMode.AliasCell);
        XPointer<PhysPresetAsset> physPresetPointer = context.PointerReader.ReadPointer<PhysPresetAsset>(rowCursor, XPointerResolutionMode.AliasCell);
        float invHighMipRadius = ReadSingle(rowCursor);
        float shatteredInvHighMipRadius = ReadSingle(rowCursor);

        if (rowCursor.Offset - start != FxGlassDef.SerializedSize)
            throw new InvalidDataException($"FxGlassDef consumed 0x{rowCursor.Offset - start:X} bytes instead of 0x{FxGlassDef.SerializedSize:X}.");

        int childSourceStart = cursor.Offset;
        context.Diagnostics.Trace(
            $"      FxGlassDef[{index}] row={rowAddress} halfThickness={halfThickness:R} color=0x{color:X8} " +
            $"material=0x{materialPointer.Raw:X8}/{materialPointer.Untyped.Type} " +
            $"materialShattered=0x{materialShatteredPointer.Raw:X8}/{materialShatteredPointer.Untyped.Type} " +
            $"physPreset=0x{physPresetPointer.Raw:X8}/{physPresetPointer.Untyped.Type} " +
            $"invHighMipRadius={invHighMipRadius:R} shatteredInvHighMipRadius={shatteredInvHighMipRadius:R} " +
            $"childSourceBegin=0x{childSourceStart:X}");

        PhysPresetAsset? physPreset = ReadPhysPresetPointer(cursor, physPresetPointer.Untyped, context);
        MaterialAsset? material = _materialLoader.LoadFromPointer(cursor, materialPointer.Untyped, context);
        MaterialAsset? materialShattered = _materialLoader.LoadFromPointer(cursor, materialShatteredPointer.Untyped, context);
        context.Diagnostics.Trace(
            $"      FxGlassDef[{index}] childSourceEnd=0x{cursor.Offset:X} " +
            $"consumed=0x{cursor.Offset - childSourceStart:X} physPreset={physPreset?.Name ?? "<null>"} " +
            $"material={material?.Info.Name ?? "<null>"} materialShattered={materialShattered?.Info.Name ?? "<null>"}");

        return new FxGlassDef
        {
            Offset = rowAddress.Offset,
            HalfThickness = halfThickness,
            TexVecs = [texVec0, texVec1],
            Color = color,
            MaterialPointer = materialPointer,
            Material = material,
            MaterialShatteredPointer = materialShatteredPointer,
            MaterialShattered = materialShattered,
            PhysPresetPointer = physPresetPointer,
            PhysPreset = physPreset,
            InvHighMipRadius = invHighMipRadius,
            ShatteredInvHighMipRadius = shatteredInvHighMipRadius
        };
    }

    private static PhysPresetAsset? ReadPhysPresetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<PhysPresetAsset>(pointer, PhysPresetAsset.SerializedSize, "PhysPreset");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new NotSupportedException($"PhysPreset pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            int sourceOffset = cursor.Offset;
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, PhysPresetAsset.SerializedSize, out XBlockAddress loadedAddress, "PhysPreset");
            if (loadedAddress != rootAddress)
                throw new InvalidDataException($"PhysPreset pointer patched to {rootAddress}, but root loaded at {loadedAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
            int type = rootCursor.ReadInt32();
            float mass = ReadSingle(rootCursor);
            float bounce = ReadSingle(rootCursor);
            float friction = ReadSingle(rootCursor);
            float bulletForceScale = ReadSingle(rootCursor);
            float explosiveForceScale = ReadSingle(rootCursor);
            XPointer<string> sndAliasPrefixPointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
            float piecesSpreadFraction = ReadSingle(rootCursor);
            float piecesUpwardVelocity = ReadSingle(rootCursor);
            byte tempDefaultToCylinder = rootCursor.ReadByte();
            byte perSurfaceSndAlias = rootCursor.ReadByte();
            ushort pad2A = rootCursor.ReadUInt16();

            if (rootCursor.Offset != PhysPresetAsset.SerializedSize)
                throw new InvalidDataException($"PhysPreset consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PhysPresetAsset.SerializedSize:X}.");

            string? name;
            string? sndAliasPrefix;
            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                name = context.PointerReader.LoadXString(cursor, namePointer);
                sndAliasPrefix = context.PointerReader.LoadXString(cursor, sndAliasPrefixPointer);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return new PhysPresetAsset
            {
                Offset = sourceOffset,
                NamePointer = namePointer,
                Name = name,
                Type = type,
                Mass = mass,
                Bounce = bounce,
                Friction = friction,
                BulletForceScale = bulletForceScale,
                ExplosiveForceScale = explosiveForceScale,
                SndAliasPrefixPointer = sndAliasPrefixPointer,
                SndAliasPrefix = sndAliasPrefix,
                PiecesSpreadFraction = piecesSpreadFraction,
                PiecesUpwardVelocity = piecesUpwardVelocity,
                TempDefaultToCylinder = tempDefaultToCylinder,
                PerSurfaceSndAlias = perSurfaceSndAlias,
                Pad2A = pad2A
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static IReadOnlyList<FxGlassPiecePlace> ReadPiecePlaces(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, FxGlassPiecePlace.SerializedSize, 4, context, "FxGlassSystem.piecePlaces");
        var c = new FastFileCursor(bytes);
        var rows = new FxGlassPiecePlace[count];
        for (int i = 0; i < rows.Length; i++)
        {
            FxSpatialFrame frame = ReadSpatialFrame(c);
            float radius = ReadSingle(c);
            uint nextFree = BitConverter.SingleToUInt32Bits(frame.Quat.X);
            rows[i] = new FxGlassPiecePlace(frame, radius, nextFree);
        }

        return rows;
    }

    private static IReadOnlyList<FxGlassPieceState> ReadPieceStates(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, FxGlassPieceState.SerializedSize, 4, context, "FxGlassSystem.pieceStates");
        var c = new FastFileCursor(bytes);
        var rows = new FxGlassPieceState[count];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new FxGlassPieceState
            {
                TexCoordOrigin = ReadVec2(c),
                SupportMask = c.ReadUInt32(),
                InitIndex = c.ReadUInt16(),
                GeoDataStart = c.ReadUInt16(),
                DefIndex = c.ReadByte(),
                Pad11 = c.ReadBytes(5),
                VertCount = c.ReadByte(),
                HoleDataCount = c.ReadByte(),
                CrackDataCount = c.ReadByte(),
                FanDataCount = c.ReadByte(),
                Flags = c.ReadUInt16(),
                AreaX2 = ReadSingle(c)
            };
        }

        return rows;
    }

    private static IReadOnlyList<FxGlassPieceDynamics> ReadPieceDynamics(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, FxGlassPieceDynamics.SerializedSize, 4, context, "FxGlassSystem.pieceDynamics");
        var c = new FastFileCursor(bytes);
        var rows = new FxGlassPieceDynamics[count];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new FxGlassPieceDynamics(
                c.ReadInt32(),
                c.ReadInt32(),
                c.ReadInt32(),
                ReadVec3(c),
                ReadVec3(c));
        }

        return rows;
    }

    private static IReadOnlyList<FxGlassInitPieceState> ReadInitPieceStates(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, FxGlassInitPieceState.SerializedSize, 4, context, "FxGlassSystem.initPieceStates");
        var c = new FastFileCursor(bytes);
        var rows = new FxGlassInitPieceState[count];
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new FxGlassInitPieceState
            {
                Frame = ReadSpatialFrame(c),
                Radius = ReadSingle(c),
                TexCoordOrigin = ReadVec2(c),
                SupportMask = c.ReadUInt32(),
                AreaX2 = ReadSingle(c),
                DefIndex = c.ReadByte(),
                VertCount = c.ReadByte(),
                FanDataCount = c.ReadByte(),
                Pad33 = c.ReadByte()
            };
        }

        return rows;
    }

    private static IReadOnlyList<FxGlassGeometryData> ReadGeometryData(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context,
        string memberName)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, FxGlassGeometryData.SerializedSize, 4, context, memberName);
        var c = new FastFileCursor(bytes);
        var rows = new FxGlassGeometryData[count];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = new FxGlassGeometryData(c.ReadUInt32());

        return rows;
    }

    private static IReadOnlyList<uint> ReadUInt32Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, sizeof(uint), alignment, context, memberName);
        var c = new FastFileCursor(bytes);
        var values = new uint[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = c.ReadUInt32();

        return values;
    }

    private static IReadOnlyList<ushort> ReadUInt16Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, sizeof(ushort), alignment, context, memberName);
        var c = new FastFileCursor(bytes);
        var values = new ushort[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = c.ReadUInt16();

        return values;
    }

    private static IReadOnlyList<float> ReadFloatArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, sizeof(float), alignment, context, memberName);
        var c = new FastFileCursor(bytes);
        var values = new float[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadSingle(c);

        return values;
    }

    private static IReadOnlyList<byte> ReadByteArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        return LoadInlineArray(cursor, pointer, count, 1, alignment, context, memberName);
    }

    private static IReadOnlyList<FxVec3> ReadVec3Array(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        byte[] bytes = LoadInlineArray(cursor, pointer, count, 0x0C, 4, context, "FxGlassSystem.linkOrg");
        var c = new FastFileCursor(bytes);
        var values = new FxVec3[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadVec3(c);

        return values;
    }

    private static XBlockAddress PatchRequiredInlineArray(
        XPointerReference pointer,
        int count,
        FastFileLoadContext context,
        string memberName)
    {
        if (pointer.Type == PointerType.Null && count == 0)
            return context.Blocks.CurrentAddress;

        if (pointer.Type == PointerType.Null)
            throw new InvalidDataException($"{memberName} is null with non-zero count {count}.");

        if (pointer.Type == PointerType.Offset)
            throw new InvalidDataException($"{memberName} pointer 0x{pointer.Raw:X8} is packed, but PS3 Load_FxGlassSystem only proves null/non-null inline array loading.");

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"{memberName} pointer 0x{pointer.Raw:X8} is not inline/insert/null.");

        return context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
    }

    private static byte[] LoadInlineArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int stride,
        int alignment,
        FastFileLoadContext context,
        string memberName)
    {
        if (pointer.Type == PointerType.Null)
        {
            if (count != 0)
                throw new InvalidDataException($"{memberName} is null with non-zero count {count}.");

            return [];
        }

        if (pointer.Type == PointerType.Offset)
            throw new InvalidDataException($"{memberName} pointer 0x{pointer.Raw:X8} is packed, but PS3 Load_FxGlassSystem only proves null/non-null inline array loading.");

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"{memberName} pointer 0x{pointer.Raw:X8} is not inline/insert/null.");

        int byteCount = checked(count * stride);
        int sourceStart = cursor.Offset;
        XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress loadedAddress, memberName);
        if (loadedAddress != targetAddress)
            throw new InvalidDataException($"{memberName} pointer patched to {targetAddress}, but array loaded at {loadedAddress}.");

        TraceArrayLoad(
            context,
            memberName,
            pointer,
            count,
            stride,
            alignment,
            sourceStart,
            cursor.Offset,
            byteCount,
            targetAddress,
            bytes);
        return bytes;
    }

    private static void TraceFxGlassHeader(
        FastFileLoadContext context,
        int sourceOffset,
        FxGlassSystem header)
    {
        context.Diagnostics.Trace(
            $"    FxGlassSystem.header source=0x{sourceOffset:X}..0x{sourceOffset + FxGlassSystem.SerializedSize:X} " +
            $"time={header.Time} prevTime={header.PrevTime} defCount={header.DefCount} pieceLimit={header.PieceLimit} " +
            $"pieceWordCount={header.PieceWordCount} initPieceCount={header.InitPieceCount} cellCount={header.CellCount} " +
            $"activePieceCount={header.ActivePieceCount} firstFreePiece=0x{header.FirstFreePiece:X8} " +
            $"geoDataLimit={header.GeoDataLimit} geoDataCount={header.GeoDataCount} initGeoDataCount={header.InitGeoDataCount} " +
            $"needToCompactData={header.NeedToCompactData} initCount={header.InitCount} pad66=0x{header.Pad66:X4} " +
            $"effectChanceAccum={header.EffectChanceAccum:R} lastPieceDeletionTime={header.LastPieceDeletionTime}");
        context.Diagnostics.Trace(
            $"    FxGlassSystem.pointers defs=0x{header.DefsPointer.Raw:X8}/{header.DefsPointer.Untyped.Type} " +
            $"piecePlaces=0x{header.PiecePlacesPointer.Raw:X8}/{header.PiecePlacesPointer.Untyped.Type} " +
            $"pieceStates=0x{header.PieceStatesPointer.Raw:X8}/{header.PieceStatesPointer.Untyped.Type} " +
            $"pieceDynamics=0x{header.PieceDynamicsPointer.Raw:X8}/{header.PieceDynamicsPointer.Untyped.Type} " +
            $"geoData=0x{header.GeoDataPointer.Raw:X8}/{header.GeoDataPointer.Untyped.Type} " +
            $"isInUse=0x{header.IsInUsePointer.Raw:X8}/{header.IsInUsePointer.Untyped.Type} " +
            $"cellBits=0x{header.CellBitsPointer.Raw:X8}/{header.CellBitsPointer.Untyped.Type} " +
            $"visData=0x{header.VisDataPointer.Raw:X8}/{header.VisDataPointer.Untyped.Type} " +
            $"linkOrg=0x{header.LinkOrgPointer.Raw:X8}/{header.LinkOrgPointer.Untyped.Type} " +
            $"halfThickness=0x{header.HalfThicknessPointer.Raw:X8}/{header.HalfThicknessPointer.Untyped.Type} " +
            $"lightingHandles=0x{header.LightingHandlesPointer.Raw:X8}/{header.LightingHandlesPointer.Untyped.Type} " +
            $"initPieceStates=0x{header.InitPieceStatesPointer.Raw:X8}/{header.InitPieceStatesPointer.Untyped.Type} " +
            $"initGeoData=0x{header.InitGeoDataPointer.Raw:X8}/{header.InitGeoDataPointer.Untyped.Type}");
    }

    private static void TraceArrayLoad(
        FastFileLoadContext context,
        string memberName,
        XPointerReference pointer,
        int count,
        int stride,
        int alignment,
        int sourceStart,
        int sourceEnd,
        int byteCount,
        XBlockAddress targetAddress,
        ReadOnlySpan<byte> bytes)
    {
        context.Diagnostics.Trace(
            $"      {memberName}.load source=0x{sourceStart:X}..0x{sourceEnd:X} " +
            $"ptr=0x{pointer.Raw:X8}/{pointer.Type} count={count} stride=0x{stride:X} " +
            $"align={alignment} bytes=0x{byteCount:X} target={targetAddress} " +
            $"preview={PreviewBytes(bytes, 32)}");
    }

    private static string PreviewBytes(ReadOnlySpan<byte> bytes, int maxBytes)
    {
        if (bytes.IsEmpty)
            return "<empty>";

        int headCount = Math.Min(bytes.Length, maxBytes);
        string head = Convert.ToHexString(bytes[..headCount]);
        if (bytes.Length <= maxBytes)
            return head;

        int tailCount = Math.Min(bytes.Length - headCount, maxBytes);
        string tail = Convert.ToHexString(bytes[^tailCount..]);
        return $"{head}...{tail}";
    }

    private static FxSpatialFrame ReadSpatialFrame(FastFileCursor cursor)
    {
        return new FxSpatialFrame(
            new FxQuat(ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor)),
            ReadVec3(cursor));
    }

    private static FxVec3 ReadVec3(FastFileCursor cursor)
    {
        return new FxVec3(ReadSingle(cursor), ReadSingle(cursor), ReadSingle(cursor));
    }

    private static FxVec2 ReadVec2(FastFileCursor cursor)
    {
        return new FxVec2(ReadSingle(cursor), ReadSingle(cursor));
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private static int Count(uint value, string name)
    {
        if (value > int.MaxValue)
            throw new InvalidDataException($"{name} {value} exceeds supported managed count range.");

        return (int)value;
    }

    private static int Align(int value, int alignment)
    {
        return checked((value + alignment - 1) / alignment * alignment);
    }
}
