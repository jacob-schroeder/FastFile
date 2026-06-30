using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Loaders.Assets.TechniqueSet;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using MaterialAssetModel = FastFile.Models.Assets.Material.MaterialAsset;

namespace FastFile.Loaders.Assets.Material;

public sealed class MaterialLoader
{
    private const int MaterialSize = 0xa8;
    private const int TechniqueSlotCount = 37;
    private const int TechniqueSetSize = 0x9c;
    private const int TechniqueSize = 0x08;
    private const int PassSize = 0x18;
    private const int VertexDeclSize = 0x1c;
    private const int VertexShaderSize = 0x0c;
    private const int PixelShaderSize = 0x18;
    private const int ShaderArgSize = 0x08;
    private const int TextureDefSize = 0x0c;
    private const int ConstantDefSize = 0x20;
    private const int GfxStateBitsSize = 0x08;
    private const int GfxImageSize = 0x50;
    private const int WaterSize = 0x48;

    public MaterialAssetModel LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level Material pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        return LoadInlineMaterial(cursor, pointer, context);
    }

    public MaterialAssetModel? LoadFromPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialAssetModel>(pointer, MaterialSize, "Material");
            return null;
        }

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        MaterialAssetModel material = LoadInlineMaterial(cursor, pointer, context, out XBlockAddress rootAddress);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

        return material;
    }

    private static MaterialAssetModel LoadInlineMaterial(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context,
        out XBlockAddress rootAddress)
    {
        int offset = cursor.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, MaterialSize, out rootAddress);
            if (rootAddress != targetAddress)
                throw new InvalidDataException($"Material pointer patched to {targetAddress}, but root loaded at {rootAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
            byte gameFlags = rootCursor.ReadByte();
            byte sortKey = rootCursor.ReadByte();
            byte textureAtlasRowCount = rootCursor.ReadByte();
            byte textureAtlasColumnCount = rootCursor.ReadByte();
            ulong drawSurfPacked = rootCursor.ReadUInt64();
            uint surfaceTypeBits = rootCursor.ReadUInt32();
            ushort hashIndex = rootCursor.ReadUInt16();
            ushort materialInfoPad16 = rootCursor.ReadUInt16();
            MaterialStateBitsEntry[] stateBitsEntries = ReadStateBitsEntries(rootCursor, TechniqueSlotCount);
            byte textureCount = rootCursor.ReadByte();
            byte constantCount = rootCursor.ReadByte();
            byte stateBitsCount = rootCursor.ReadByte();
            byte stateFlags = rootCursor.ReadByte();
            byte cameraRegion = rootCursor.ReadByte();
            byte xstringCount = rootCursor.ReadByte();
            byte pad43 = rootCursor.ReadByte();
            ushort[] inlineTechniqueSlotStateBits = ReadUshorts(rootCursor, TechniqueSlotCount);
            ushort pad8E = rootCursor.ReadUInt16();
            XPointerReference runtimeUshortPayload = ReadRawCell(rootCursor, XPointerOffsetMode.Direct);
            XPointerReference techniqueSetPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);
            XPointerReference textureTablePointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
            XPointerReference constantTablePointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
            XPointerReference stateBitsPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
            XPointerReference xstringTablePointer = ReadRawCell(rootCursor, XPointerOffsetMode.Direct);

            if (rootCursor.Offset != MaterialSize)
                throw new InvalidDataException($"Material consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MaterialSize:X}.");

            context.Diagnostics.Trace(
                $"  Material root source=0x{offset:X} name=0x{namePointer.Raw:X8} textures={textureCount} constants={constantCount} " +
                $"stateBits={stateBitsCount} xstrings={xstringCount} runtimeUshort=0x{runtimeUshortPayload.Raw:X8} techset=0x{techniqueSetPointer.Raw:X8} " +
                $"texturesPtr=0x{textureTablePointer.Raw:X8} constantsPtr=0x{constantTablePointer.Raw:X8} stateBitsPtr=0x{stateBitsPointer.Raw:X8} xstringsPtr=0x{xstringTablePointer.Raw:X8} " +
                $"blocks={context.Blocks.DescribePositions()}");

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                string? name = ReadXString(cursor, namePointer, context);
                IReadOnlyList<ushort> runtimeTechniqueSlotStateBits = ReadRuntimeUshortPayload(cursor, runtimeUshortPayload, context);
                MaterialTechniqueSetAsset? techniqueSet = ReadTechniqueSetPointer(cursor, techniqueSetPointer, context);
                IReadOnlyList<MaterialTextureDef> textures = ReadTextureDefArray(cursor, textureTablePointer, textureCount, context);
                IReadOnlyList<MaterialConstantDef> constants = ReadMaterialConstantArray(cursor, constantTablePointer, constantCount, context);
                IReadOnlyList<GfxStateBits> stateBits = ReadGfxStateBitsArray(cursor, stateBitsPointer, stateBitsCount, context);
                IReadOnlyList<MaterialXStringEntry> xstrings = ReadXStringPointerArray(cursor, xstringTablePointer, xstringCount, context);

                context.Diagnostics.Trace(
                    $"  Material end source=0x{cursor.Offset:X} rootSource=0x{offset:X} blocks={context.Blocks.DescribePositions()}");

                return new MaterialAssetModel
                {
                    Offset = offset,
                    Info = new MaterialInfo
                    {
                        NamePointer = namePointer,
                        Name = name,
                        GameFlags = gameFlags,
                        SortKey = sortKey,
                        TextureAtlasRowCount = textureAtlasRowCount,
                        TextureAtlasColumnCount = textureAtlasColumnCount,
                        DrawSurf = new GfxDrawSurf(drawSurfPacked),
                        SurfaceTypeBits = surfaceTypeBits,
                        HashIndex = hashIndex,
                        Pad16 = materialInfoPad16
                    },
                    StateBitsEntries = stateBitsEntries,
                    TextureCount = textureCount,
                    ConstantCount = constantCount,
                    StateBitsCount = stateBitsCount,
                    StateFlags = stateFlags,
                    CameraRegion = cameraRegion,
                    XStringCount = xstringCount,
                    Pad43 = pad43,
                    InlineTechniqueSlotStateBits = inlineTechniqueSlotStateBits,
                    Pad8E = pad8E,
                    RuntimeTechniqueSlotStateBitsPointer = runtimeUshortPayload,
                    RuntimeTechniqueSlotStateBits = runtimeTechniqueSlotStateBits,
                    TechniqueSetPointer = techniqueSetPointer.AsPointer<MaterialTechniqueSetAsset>(),
                    TechniqueSet = techniqueSet,
                    TextureTablePointer = textureTablePointer,
                    Textures = textures,
                    ConstantTablePointer = constantTablePointer,
                    Constants = constants,
                    StateBitsPointer = stateBitsPointer,
                    StateBits = stateBits,
                    XStringTablePointer = xstringTablePointer,
                    XStrings = xstrings
                };
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialAssetModel LoadInlineMaterial(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        return LoadInlineMaterial(cursor, pointer, context, out _);
    }

    private static MaterialStateBitsEntry[] ReadStateBitsEntries(FastFileCursor cursor, int count)
    {
        var entries = new MaterialStateBitsEntry[count];
        for (int i = 0; i < entries.Length; i++)
            entries[i] = new MaterialStateBitsEntry(i, cursor.ReadByte());

        return entries;
    }

    private static IReadOnlyList<ushort> ReadRuntimeUshortPayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        int byteCount = TechniqueSlotCount * sizeof(ushort);
        if (pointer.Type == PointerType.Null)
            return [];

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Material runtime ushort payload cell 0x{pointer.Raw:X8} has no destination cell address.");

        context.Blocks.Push(XFileBlockType.RUNTIME);
        try
        {
            context.Blocks.AlignCurrent(2);
            XBlockAddress payloadAddress = context.Blocks.CurrentAddress;
            context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(payloadAddress));
            byte[] payloadBytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress loadedAddress);
            var payloadCursor = new FastFileCursor(payloadBytes, loadedAddress);
            return ReadUshorts(payloadCursor, TechniqueSlotCount);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialTechniqueSetAsset? ReadTechniqueSetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialTechniqueSetAsset>(pointer, TechniqueSetSize, "MaterialTechniqueSet");
            return null;
        }

        return new MaterialTechniqueSetLoader().LoadFromAssetPointer(cursor, pointer, context);
    }

    private static void ReadTechniquePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialTechniqueAsset>(pointer, TechniqueSize, "MaterialTechnique");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        rootCursor.ReadUInt16();
        int passCount = rootCursor.ReadUInt16();

        if (rootCursor.Offset != TechniqueSize)
            throw new InvalidDataException($"MaterialTechnique consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSize:X}.");

        context.Diagnostics.Trace(
            $"      Material.inlineTechnique root name=0x{namePointer.Raw:X8} passCount={passCount} blocks={context.Blocks.DescribePositions()}");

        var passes = new MaterialPassRoot[passCount];
        for (int i = 0; i < passes.Length; i++)
            passes[i] = ReadPassRoot(cursor, context);

        foreach (MaterialPassRoot pass in passes)
            ReadPassChildren(cursor, pass, context);

        ReadXString(cursor, namePointer, context);
    }

    private static MaterialPassRoot ReadPassRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        byte[] rootBytes = context.Blocks.Load(cursor, PassSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var pass = new MaterialPassRoot(
            context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct),
            context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell),
            context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell),
            rootCursor.ReadByte(),
            rootCursor.ReadByte(),
            rootCursor.ReadByte(),
            rootCursor.ReadByte(),
            rootCursor.ReadByte());
        rootCursor.Skip(3);
        pass = pass with
        {
            Args = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct)
        };

        if (rootCursor.Offset != PassSize)
            throw new InvalidDataException($"MaterialPass consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PassSize:X}.");

        context.Diagnostics.Trace(
            $"        Material.inlinePass root vd={pass.VertexDecl} vs={pass.VertexShader} ps={pass.PixelShader} " +
            $"args={pass.PerPrimArgCount}+{pass.PerObjArgCount}+{pass.StableArgCount} args={pass.Args} blocks={context.Blocks.DescribePositions()}");

        return pass;
    }

    private static void ReadPassChildren(
        FastFileCursor cursor,
        MaterialPassRoot pass,
        FastFileLoadContext context)
    {
        ReadFixedObject(cursor, pass.VertexDecl, VertexDeclSize, 4, context);
        ReadShaderPointer(cursor, pass.VertexShader, VertexShaderSize, context);
        ReadShaderPointer(cursor, pass.PixelShader, PixelShaderSize, context);
        ReadShaderArgArray(
            cursor,
            pass.Args,
            pass.PerPrimArgCount + pass.PerObjArgCount + pass.StableArgCount,
            context);
    }

    private static void ReadShaderPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        int rootSize,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialShaderAsset>(pointer, rootSize, "MaterialShader");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, rootSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        XPointerReference dataPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);
        uint dataSize = rootCursor.ReadUInt32();

        ReadXString(cursor, namePointer, context);
        ReadSizedBytesPointer(cursor, dataPointer, dataSize, 16, context);
    }

    private static void ReadShaderArgArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative MaterialShaderArgument count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialShaderArgumentAsset[]>(pointer, checked(count * ShaderArgSize), "MaterialShaderArgument[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] argBytes = context.Blocks.Load(cursor, checked(count * ShaderArgSize), out XBlockAddress argAddress);
        var argCursor = new FastFileCursor(argBytes, argAddress);
        var args = new (ushort Type, XPointerReference Pointer)[count];

        for (int i = 0; i < args.Length; i++)
        {
            ushort type = argCursor.ReadUInt16();
            argCursor.ReadUInt16();
            XPointerReference pointerCell = context.PointerReader.ReadCell(argCursor, XPointerOffsetMode.Direct);
            args[i] = (type, pointerCell);
        }

        foreach ((ushort type, XPointerReference argumentPointer) in args)
        {
            if (type is 1 or 7 && context.PointerReader.HasInlinePayload(argumentPointer))
            {
                context.PointerReader.PatchInlinePointerCell(argumentPointer, alignment: 16);
                context.Blocks.Load(cursor, 0x10);
            }
        }
    }

    private static IReadOnlyList<MaterialTextureDef> ReadTextureDefArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative MaterialTextureDef count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialTextureDef[]>(pointer, checked(count * TextureDefSize), "MaterialTextureDef[]");
            return [];
        }

        context.Diagnostics.Trace(
            $"    Material.textureDefs table source=0x{cursor.Offset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] textureBytes = context.Blocks.Load(cursor, checked(count * TextureDefSize), out XBlockAddress textureAddress);
        var textureCursor = new FastFileCursor(textureBytes, textureAddress);
        var textures = new MaterialTextureDef[count];

        for (int i = 0; i < textures.Length; i++)
        {
            uint nameHash = textureCursor.ReadUInt32();
            byte nameStart = textureCursor.ReadByte();
            byte nameEnd = textureCursor.ReadByte();
            byte samplerState = textureCursor.ReadByte();
            byte semantic = textureCursor.ReadByte();
            XPointerReference dataPointer = context.PointerReader.ReadCell(textureCursor, XPointerOffsetMode.AliasCell);
            textures[i] = new MaterialTextureDef
            {
                NameHash = nameHash,
                NameStart = nameStart,
                NameEnd = nameEnd,
                SamplerState = samplerState,
                Semantic = semantic,
                DataPointer = dataPointer
            };
        }

        for (int i = 0; i < textures.Length; i++)
        {
            MaterialTextureDef texture = textures[i];
            context.Diagnostics.Trace(
                $"      Material.texture semantic=0x{texture.Semantic:X2} data=0x{texture.DataPointer.Raw:X8} mode={texture.DataPointer.ResolutionMode} source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");

            if (texture.Semantic == 0x0b)
                textures[i] = CopyTexture(texture, water: ReadWaterPointer(cursor, texture.DataPointer, context));
            else
                textures[i] = CopyTexture(texture, image: ReadGfxImagePointer(cursor, texture.DataPointer, context));
        }

        return textures;
    }

    private static MaterialTextureDef CopyTexture(
        MaterialTextureDef texture,
        GfxImageAsset? image = null,
        MaterialWater? water = null)
    {
        return new MaterialTextureDef
        {
            NameHash = texture.NameHash,
            NameStart = texture.NameStart,
            NameEnd = texture.NameEnd,
            SamplerState = texture.SamplerState,
            Semantic = texture.Semantic,
            DataPointer = texture.DataPointer,
            Image = image,
            Water = water
        };
    }

    private static GfxImageAsset? ReadGfxImagePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<GfxImageAsset>(pointer, GfxImageSize, "GfxImage");
            return null;
        }

        int sourceOffset = cursor.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, GfxImageSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            byte formatByte = rootCursor.ReadByte();
            byte levelCount = rootCursor.ReadByte();
            byte dimensionCount = rootCursor.ReadByte();
            byte multiFaceControl = rootCursor.ReadByte();
            uint textureFlags = rootCursor.ReadUInt32();
            ushort width = rootCursor.ReadUInt16();
            ushort height = rootCursor.ReadUInt16();
            ushort depth = rootCursor.ReadUInt16();
            byte pixelDataBlock = rootCursor.ReadByte();
            byte pad0F = rootCursor.ReadByte();
            uint renderTargetPitch = rootCursor.ReadUInt32();
            uint pixelsOffset = rootCursor.ReadUInt32();
            byte mapType = rootCursor.ReadByte();
            byte textureSemantic = rootCursor.ReadByte();
            byte category = rootCursor.ReadByte();
            byte pad1B = rootCursor.ReadByte();
            uint cardMemory = rootCursor.ReadUInt32();
            ushort baseWidth = rootCursor.ReadUInt16();
            ushort baseHeight = rootCursor.ReadUInt16();
            ushort baseDepth = rootCursor.ReadUInt16();
            byte baseLevelCount = rootCursor.ReadByte();
            byte pad27 = rootCursor.ReadByte();
            XPointerReference payloadPointer = ReadRawCell(rootCursor, XPointerOffsetMode.Direct);
            byte[] streamData = rootCursor.ReadBytes(0x20);
            XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);

            if (rootCursor.Offset != GfxImageSize)
                throw new InvalidDataException($"GfxImage consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GfxImageSize:X}.");

            context.Diagnostics.Trace(
                $"      GfxImage root.pre source=0x{cursor.Offset:X} root={rootAddress} format=0x{formatByte:X2} flags=0x{textureFlags:X8} " +
                $"dims={width}x{height}x{depth} levels={levelCount} dimension=0x{dimensionCount:X2} map=0x{mapType:X2} semantic=0x{textureSemantic:X2} " +
                $"category=0x{category:X2} " +
                $"baseDims={baseWidth}x{baseHeight}x{baseDepth} baseLevels={baseLevelCount} cardMemory=0x{cardMemory:X8} " +
                $"pixelBlock=0x{pixelDataBlock:X2} pitch=0x{renderTargetPitch:X8} pixelsOffset=0x{pixelsOffset:X8} " +
                $"pad1B=0x{pad1B:X2} streamData={Convert.ToHexString(streamData)} " +
                $"payload=0x{payloadPointer.Raw:X8} name=0x{namePointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

            context.Diagnostics.Trace(
                $"      GfxImage root format=0x{formatByte:X2} flags=0x{textureFlags:X8} dims={width}x{height}x{depth} " +
                $"levels={levelCount} dimension=0x{dimensionCount:X2} map=0x{mapType:X2} semantic=0x{textureSemantic:X2} " +
                $"category=0x{category:X2} " +
                $"baseDims={baseWidth}x{baseHeight}x{baseDepth} baseLevels={baseLevelCount} cardMemory=0x{cardMemory:X8} " +
                $"pad1B=0x{pad1B:X2} streamData={Convert.ToHexString(streamData)} payload=0x{payloadPointer.Raw:X8} " +
                $"name=0x{namePointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                string? name = ReadXString(cursor, namePointer, context);
                int payloadByteCount = ReadGfxImagePayload(
                    cursor,
                    payloadPointer,
                    formatByte,
                    levelCount,
                    multiFaceControl,
                    textureFlags,
                    width,
                    height,
                    depth,
                    textureSemantic,
                    context);

                return new GfxImageAsset
                {
                    Offset = sourceOffset,
                    Format = formatByte,
                    LevelCount = levelCount,
                    DimensionCount = dimensionCount,
                    MultiFaceControl = multiFaceControl,
                    TextureFlags = textureFlags,
                    Width = width,
                    Height = height,
                    Depth = depth,
                    PixelDataBlock = pixelDataBlock,
                    Pad0F = pad0F,
                    RenderTargetPitch = renderTargetPitch,
                    PixelsOffset = pixelsOffset,
                    MapType = mapType,
                    TextureSemantic = textureSemantic,
                    Category = category,
                    Pad1B = pad1B,
                    CardMemory = cardMemory,
                    BaseWidth = baseWidth,
                    BaseHeight = baseHeight,
                    BaseDepth = baseDepth,
                    BaseLevelCount = baseLevelCount,
                    Pad27 = pad27,
                    PayloadPointer = payloadPointer,
                    StreamData = ReadGfxImageStreamData(streamData),
                    PayloadByteCount = payloadByteCount,
                    NamePointer = namePointer,
                    Name = name
                };
            }
            finally
            {
                context.Blocks.Pop();
            }
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static int ReadGfxImagePayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        byte formatByte,
        byte levelCount,
        byte multiFaceControl,
        uint textureFlags,
        ushort width,
        ushort height,
        ushort depth,
        byte textureSemantic,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return 0;

        int byteCount = ComputeGfxImagePayloadByteCount(
            formatByte,
            levelCount,
            multiFaceControl,
            textureFlags,
            width,
            height,
            depth);

        XFileBlockType payloadBlock = textureSemantic == 0x0b
            ? XFileBlockType.RUNTIME
            : XFileBlockType.PHYSICAL;

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"GfxImage payload pointer 0x{pointer.Raw:X8} has no destination cell address.");

        context.Blocks.Push(payloadBlock);
        try
        {
            context.Blocks.AlignCurrent(128);
            XBlockAddress payloadAddress = context.Blocks.CurrentAddress;
            context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(payloadAddress));
            context.Blocks.Load(cursor, byteCount);
            context.Diagnostics.Trace(
                $"        GfxImage payload block={payloadBlock} target={payloadAddress} bytes=0x{byteCount:X} blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }

        return byteCount;
    }

    private static int ComputeGfxImagePayloadByteCount(
        byte formatByte,
        byte levelCount,
        byte multiFaceControl,
        uint textureFlags,
        ushort width,
        ushort height,
        ushort depth)
    {
        byte normalizedFormat = (byte)(formatByte & 0xdf);
        uint formatKey = (textureFlags << 8) | normalizedFormat;
        long total = 0;

        for (int level = 0; level < levelCount; level++)
        {
            int levelWidth = Math.Max(1, width >> level);
            int levelHeight = Math.Max(1, height >> level);
            int levelDepth = Math.Max(1, depth >> level);
            total += ComputeGfxImageMipByteCount(formatKey, levelWidth, levelHeight, levelDepth);
        }

        total = Align(total, 128);
        if (multiFaceControl != 0)
            total = Align(checked(total * 6), 128);

        if (total > int.MaxValue)
            throw new InvalidDataException($"GfxImage payload size 0x{total:X} does not fit in this loader.");

        return (int)total;
    }

    private static long ComputeGfxImageMipByteCount(uint formatKey, int width, int height, int depth)
    {
        return formatKey switch
        {
            0x01AAE485 or
            0x01AAE490 or
            0x01AAE49C or
            0x01AAE49E or
            0x00AAFE9F => checked((long)width * height * depth * 4),

            0x01AAE492 or
            0x01AAAB8B => checked((long)width * height * depth * 2),

            0x01A9FF81 or
            0x0156FF81 => checked((long)width * height * depth),

            0x01A9AA86 or
            0x01AA5686 or
            0x0156AA86 or
            0x01AAE486 => checked((long)((width + 3) >> 2) * ((height + 3) >> 2) * depth * 8),

            0x01AAE487 or
            0x01AAE488 => checked((long)((width + 3) >> 2) * ((height + 3) >> 2) * depth * 16),

            _ => throw new NotSupportedException(
                $"Unsupported GfxImage format key 0x{formatKey:X8} for {width}x{height}x{depth} mip payload.")
        };
    }

    private static long Align(long value, int alignment)
    {
        return checked((value + alignment - 1) / alignment * alignment);
    }

    private static MaterialWater? ReadWaterPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, WaterSize, "water_t");
            return null;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, WaterSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var writable = new MaterialWaterWritable(rootCursor.ReadUInt32());
        XPointerReference h0xPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        XPointerReference h0yPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        XPointerReference wTermPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        int m = rootCursor.ReadInt32();
        int n = rootCursor.ReadInt32();
        float lx = ReadSingle(rootCursor);
        float lz = ReadSingle(rootCursor);
        float gravity = ReadSingle(rootCursor);
        float windVelocity = ReadSingle(rootCursor);
        var windDirection = new MaterialVec2(ReadSingle(rootCursor), ReadSingle(rootCursor));
        float amplitude = ReadSingle(rootCursor);
        var codeConstant = new MaterialVec4(
            ReadSingle(rootCursor),
            ReadSingle(rootCursor),
            ReadSingle(rootCursor),
            ReadSingle(rootCursor));
        XPointerReference imagePointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);

        int elementCount = checked(m * n);
        IReadOnlyList<float> h0x = ReadWaterSpectrum(cursor, h0xPointer, elementCount, context);
        IReadOnlyList<float> h0y = ReadWaterSpectrum(cursor, h0yPointer, elementCount, context);
        IReadOnlyList<float> wTerm = ReadWaterSpectrum(cursor, wTermPointer, elementCount, context);
        GfxImageAsset? image = ReadGfxImagePointer(cursor, imagePointer, context);

        return new MaterialWater
        {
            Writable = writable,
            H0XPointer = h0xPointer,
            H0YPointer = h0yPointer,
            WTermPointer = wTermPointer,
            M = m,
            N = n,
            Lx = lx,
            Lz = lz,
            Gravity = gravity,
            WindVelocity = windVelocity,
            WindDirection = windDirection,
            Amplitude = amplitude,
            CodeConstant = codeConstant,
            ImagePointer = imagePointer.AsPointer<GfxImageAsset>(),
            H0X = h0x,
            H0Y = h0y,
            WTerm = wTerm,
            Image = image
        };
    }

    private static IReadOnlyList<float> ReadWaterSpectrum(
        FastFileCursor cursor,
        XPointerReference pointer,
        int elementCount,
        FastFileLoadContext context)
    {
        int byteCount = checked(elementCount * sizeof(float));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<float[]>(pointer, byteCount, "water spectrum float[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress address);
        var spectrumCursor = new FastFileCursor(bytes, address);
        var values = new float[elementCount];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadSingle(spectrumCursor);

        return values;
    }

    private static IReadOnlyList<GfxStateBits> ReadGfxStateBitsArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative GfxStateBits count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<GfxStateBits[]>(pointer, checked(count * GfxStateBitsSize), "GfxStateBits[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] stateBytes = context.Blocks.Load(cursor, checked(count * GfxStateBitsSize), out XBlockAddress stateAddress);
        context.Diagnostics.Trace(
            $"      GfxStateBits table source=0x{cursor.Offset:X} count={count} root={stateAddress} ptr=0x{pointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");
        var stateCursor = new FastFileCursor(stateBytes, stateAddress);
        var stateBits = new GfxStateBits[count];

        for (int i = 0; i < stateBits.Length; i++)
        {
            int loadBitsCellOffset = stateCursor.Offset;
            XPointerReference loadBits = XPointerReference.FromRaw(
                stateCursor.ReadInt32(),
                XPointerResolutionMode.AliasCell,
                stateCursor.AddressAt(loadBitsCellOffset));
            uint tail = stateCursor.ReadUInt32();
            stateBits[i] = new GfxStateBits
            {
                LoadBitsPointer = loadBits,
                Tail = tail
            };
        }

        for (int i = 0; i < stateBits.Length; i++)
        {
            GfxStateBits state = stateBits[i];
            stateBits[i] = new GfxStateBits
            {
                LoadBitsPointer = state.LoadBitsPointer,
                LoadBits = ReadGfxStateBitsLoadBits(cursor, state.LoadBitsPointer, context),
                Tail = state.Tail
            };
        }

        return stateBits;
    }

    private static IReadOnlyList<MaterialConstantDef> ReadMaterialConstantArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative MaterialConstantDef count {count}.");

        int byteCount = checked(count * ConstantDefSize);
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "MaterialConstantDef[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 16);
        byte[] bytes = context.Blocks.Load(cursor, byteCount, out XBlockAddress address);
        var constantCursor = new FastFileCursor(bytes, address);
        var constants = new MaterialConstantDef[count];

        for (int i = 0; i < constants.Length; i++)
        {
            uint nameHash = constantCursor.ReadUInt32();
            byte[] nameBytes = constantCursor.ReadBytes(0x0c);
            constants[i] = new MaterialConstantDef
            {
                NameHash = nameHash,
                NameBytes = nameBytes,
                Literal = new MaterialVec4(
                    ReadSingle(constantCursor),
                    ReadSingle(constantCursor),
                    ReadSingle(constantCursor),
                    ReadSingle(constantCursor))
            };
        }

        return constants;
    }

    private static IReadOnlyList<uint> ReadGfxStateBitsLoadBits(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        const int byteCount = 2 * sizeof(int);

        context.Diagnostics.Trace(
            $"        GfxStateBits.LoadBits raw=0x{pointer.Raw:X8} mode={pointer.ResolutionMode} source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");

        if (pointer.Type == PointerType.Null)
            return [];

        if (pointer.Type == PointerType.Offset)
        {
            try
            {
                context.PointerReader.ValidateOffsetPointerRange<byte[]>(pointer, byteCount, "GfxStateBits.LoadBits");
            }
            catch (InvalidDataException)
            {
                context.Diagnostics.Trace(
                    $"      GfxStateBits raw loadBits=0x{pointer.Raw:X8} at {pointer.CellAddress} is not a materialized alias pointer.");
            }

            return [];
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return [];

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress loadBitsAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] bytes = context.Blocks.Load(cursor, byteCount);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(loadBitsAddress));

            var loadBitsCursor = new FastFileCursor(bytes, loadBitsAddress);
            return [loadBitsCursor.ReadUInt32(), loadBitsCursor.ReadUInt32()];
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static IReadOnlyList<MaterialXStringEntry> ReadXStringPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative Material XString count {count}.");

        if (pointer.Type == PointerType.Null)
            return [];

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Material XString[] cell 0x{pointer.Raw:X8} has no destination cell address.");

        context.Blocks.AlignCurrent(4);
        XBlockAddress tableAddress = context.Blocks.CurrentAddress;
        context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(tableAddress));
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress pointerTableAddress);
        if (pointerTableAddress != tableAddress)
            throw new InvalidDataException($"Material XString[] pointer patched to {tableAddress}, but table loaded at {pointerTableAddress}.");

        var pointerCursor = new FastFileCursor(pointerBytes, pointerTableAddress);
        var pointers = new XPointer<string>[count];

        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = context.PointerReader.ReadPointer<string>(pointerCursor, XPointerResolutionMode.Direct);

        var entries = new MaterialXStringEntry[count];
        for (int i = 0; i < pointers.Length; i++)
            entries[i] = new MaterialXStringEntry(i, pointers[i], ReadXString(cursor, pointers[i], context));

        return entries;
    }

    private static void ReadFixedArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        int stride,
        int alignment,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative fixed-array count {count}.");

        int byteCount = checked(count * stride);
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "fixed array");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        context.Blocks.Load(cursor, byteCount);
    }

    private static ushort[] ReadUshorts(FastFileCursor cursor, int count)
    {
        var values = new ushort[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = cursor.ReadUInt16();

        return values;
    }

    private static IReadOnlyList<GfxImageStreamData> ReadGfxImageStreamData(byte[] bytes)
    {
        var cursor = new FastFileCursor(bytes);
        var entries = new GfxImageStreamData[GfxImageStreamData.EntryCount];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new GfxImageStreamData(
                cursor.ReadUInt16(),
                cursor.ReadUInt16(),
                cursor.ReadUInt32());
        }

        return entries;
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }

    private static void ReadFixedObject(
        FastFileCursor cursor,
        XPointerReference pointer,
        int byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "fixed object");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment);
        context.Blocks.Load(cursor, byteCount);
    }

    private static void ReadSizedBytesPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        uint byteCount,
        int alignment,
        FastFileLoadContext context)
    {
        if (byteCount > int.MaxValue)
            throw new InvalidDataException($"Pointer byte count 0x{byteCount:X} does not fit in this reader.");

        context.PointerReader.LoadBytes(cursor, pointer, (int)byteCount, alignment);
    }

    private static XPointer<string> ReadXStringPointer(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return context.PointerReader.ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static string? ReadXString(
        FastFileCursor cursor,
        XPointer<string> pointer,
        FastFileLoadContext context)
    {
        return context.PointerReader.LoadXString(cursor, pointer);
    }

    private static XPointerReference ReadRawCell(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode)
    {
        int cellOffset = cursor.Offset;
        return XPointerReference.FromRaw(
            cursor.ReadInt32(),
            offsetMode,
            cursor.AddressAt(cellOffset));
    }

    private readonly record struct MaterialPassRoot(
        XPointerReference VertexDecl,
        XPointerReference VertexShader,
        XPointerReference PixelShader,
        byte PerPrimArgCount,
        byte PerObjArgCount,
        byte StableArgCount,
        byte CustomSamplerFlags,
        byte PrecompiledIndex)
    {
        public XPointerReference Args { get; init; }
    }
}
