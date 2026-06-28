using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
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
            context.PointerReader.ValidateOffsetPointerRange(pointer, MaterialSize, "Material");
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
            rootCursor.Skip(0x3d - rootCursor.Offset);
            int textureCount = rootCursor.ReadByte();
            int constantCount = rootCursor.ReadByte();
            int stateBitsCount = rootCursor.ReadByte();
            rootCursor.Skip(0x42 - rootCursor.Offset);
            int xstringCount = rootCursor.ReadByte();
            rootCursor.Skip(0x90 - rootCursor.Offset);
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
                ReadXString(cursor, namePointer, context);
                ReadRuntimeUshortPayload(cursor, runtimeUshortPayload, context);
                ReadTechniqueSetPointer(cursor, techniqueSetPointer, context);
                ReadTextureDefArray(cursor, textureTablePointer, textureCount, context);
                ReadFixedArray(cursor, constantTablePointer, constantCount, ConstantDefSize, 16, context);
                ReadGfxStateBitsArray(cursor, stateBitsPointer, stateBitsCount, context);
                ReadXStringPointerArray(cursor, xstringTablePointer, xstringCount, context);

                context.Diagnostics.Trace(
                    $"  Material end source=0x{cursor.Offset:X} rootSource=0x{offset:X} blocks={context.Blocks.DescribePositions()}");

                return new MaterialAssetModel
            {
                Offset = offset
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

    private static void ReadRuntimeUshortPayload(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        int byteCount = TechniqueSlotCount * sizeof(ushort);
        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.CellAddress is not { } cellAddress)
            throw new InvalidDataException($"Material runtime ushort payload cell 0x{pointer.Raw:X8} has no destination cell address.");

        context.Blocks.Push(XFileBlockType.RUNTIME);
        try
        {
            context.Blocks.AlignCurrent(2);
            XBlockAddress payloadAddress = context.Blocks.CurrentAddress;
            context.Blocks.WriteInt32(cellAddress, XPointerCodec.Encode(payloadAddress));
            context.Blocks.Load(cursor, byteCount);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadTechniqueSetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, TechniqueSetSize, "MaterialTechniqueSet");
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSetSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
            rootCursor.Skip(4);
            var techniquePointers = new XPointerReference[TechniqueSlotCount];
            for (int i = 0; i < techniquePointers.Length; i++)
                techniquePointers[i] = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);

            if (rootCursor.Offset != TechniqueSetSize)
                throw new InvalidDataException($"MaterialTechniqueSet consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSetSize:X}.");

            context.Diagnostics.Trace(
                $"    Material.inlineTechset root name=0x{namePointer.Raw:X8} techniques.nonNull={techniquePointers.Count(x => x.Raw != 0)} " +
                $"techniques.inline={techniquePointers.Count(x => x.Raw == -1)} blocks={context.Blocks.DescribePositions()}");

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                foreach (XPointerReference techniquePointer in techniquePointers)
                    ReadTechniquePointer(cursor, techniquePointer, context);
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

    private static void ReadTechniquePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, TechniqueSize, "MaterialTechnique");
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
            context.PointerReader.ValidateOffsetPointerRange(pointer, rootSize, "MaterialShader");
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
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * ShaderArgSize), "MaterialShaderArgument[]");
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

    private static void ReadTextureDefArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative MaterialTextureDef count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * TextureDefSize), "MaterialTextureDef[]");
            return;
        }

        context.Diagnostics.Trace(
            $"    Material.textureDefs table source=0x{cursor.Offset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] textureBytes = context.Blocks.Load(cursor, checked(count * TextureDefSize), out XBlockAddress textureAddress);
        var textureCursor = new FastFileCursor(textureBytes, textureAddress);
        var textures = new (byte Semantic, XPointerReference DataPointer)[count];

        for (int i = 0; i < textures.Length; i++)
        {
            textureCursor.ReadInt32();
            textureCursor.ReadByte();
            textureCursor.ReadByte();
            textureCursor.ReadByte();
            byte semantic = textureCursor.ReadByte();
            XPointerReference dataPointer = context.PointerReader.ReadCell(textureCursor, XPointerOffsetMode.AliasCell);
            textures[i] = (semantic, dataPointer);
        }

        foreach ((byte semantic, XPointerReference dataPointer) in textures)
        {
            context.Diagnostics.Trace(
                $"      Material.texture semantic=0x{semantic:X2} data=0x{dataPointer.Raw:X8} mode={dataPointer.ResolutionMode} source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");

            if (semantic == 0x0b)
                ReadWaterPointer(cursor, dataPointer, context);
            else
                ReadGfxImagePointer(cursor, dataPointer, context);
        }
    }

    private static void ReadGfxImagePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, GfxImageSize, "GfxImage");
            return;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, GfxImageSize, out XBlockAddress rootAddress);
            var rootCursor = new FastFileCursor(rootBytes, rootAddress);

            byte formatByte = rootCursor.ReadByte();
            byte levelCount = rootCursor.ReadByte();
            byte unknown02 = rootCursor.ReadByte();
            byte multiFaceControl = rootCursor.ReadByte();
            uint textureFlags = rootCursor.ReadUInt32();
            ushort width = rootCursor.ReadUInt16();
            ushort height = rootCursor.ReadUInt16();
            ushort depth = rootCursor.ReadUInt16();
            rootCursor.Skip(0x18 - rootCursor.Offset);
            byte mapType = rootCursor.ReadByte();
            byte textureSemantic = rootCursor.ReadByte();
            rootCursor.Skip(0x28 - rootCursor.Offset);
            XPointerReference payloadPointer = ReadRawCell(rootCursor, XPointerOffsetMode.Direct);
            rootCursor.Skip(0x4c - rootCursor.Offset);
            XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);

            if (rootCursor.Offset != GfxImageSize)
                throw new InvalidDataException($"GfxImage consumed 0x{rootCursor.Offset:X} bytes instead of 0x{GfxImageSize:X}.");

            context.Diagnostics.Trace(
                $"      GfxImage root.pre source=0x{cursor.Offset:X} root={rootAddress} format=0x{formatByte:X2} flags=0x{textureFlags:X8} " +
                $"dims={width}x{height}x{depth} levels={levelCount} map=0x{mapType:X2} semantic=0x{textureSemantic:X2} " +
                $"payload=0x{payloadPointer.Raw:X8} name=0x{namePointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

            context.Diagnostics.Trace(
                $"      GfxImage root format=0x{formatByte:X2} flags=0x{textureFlags:X8} dims={width}x{height}x{depth} " +
                $"levels={levelCount} map=0x{mapType:X2} semantic=0x{textureSemantic:X2} payload=0x{payloadPointer.Raw:X8} " +
                $"name=0x{namePointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadXString(cursor, namePointer, context);
                ReadGfxImagePayload(
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

    private static void ReadGfxImagePayload(
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
            return;

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

    private static void ReadWaterPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, WaterSize, "water_t");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, WaterSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        rootCursor.ReadInt32();
        XPointerReference firstSpectrum = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        XPointerReference secondSpectrum = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        XPointerReference thirdSpectrum = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct);
        int m = rootCursor.ReadInt32();
        int n = rootCursor.ReadInt32();
        rootCursor.Skip(0x44 - rootCursor.Offset);
        XPointerReference imagePointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);

        int elementCount = checked(m * n);
        ReadWaterSpectrum(cursor, firstSpectrum, elementCount, context);
        ReadWaterSpectrum(cursor, secondSpectrum, elementCount, context);
        ReadWaterSpectrum(cursor, thirdSpectrum, elementCount, context);
        ReadGfxImagePointer(cursor, imagePointer, context);
    }

    private static void ReadWaterSpectrum(
        FastFileCursor cursor,
        XPointerReference pointer,
        int elementCount,
        FastFileLoadContext context)
    {
        int byteCount = checked(elementCount * sizeof(float));
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "water spectrum float[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        context.Blocks.Load(cursor, byteCount);
    }

    private static void ReadGfxStateBitsArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative GfxStateBits count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * GfxStateBitsSize), "GfxStateBits[]");
            return;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] stateBytes = context.Blocks.Load(cursor, checked(count * GfxStateBitsSize), out XBlockAddress stateAddress);
        context.Diagnostics.Trace(
            $"      GfxStateBits table source=0x{cursor.Offset:X} count={count} root={stateAddress} ptr=0x{pointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");
        var stateCursor = new FastFileCursor(stateBytes, stateAddress);
        var loadBits = new XPointerReference[count];

        for (int i = 0; i < loadBits.Length; i++)
        {
            int loadBitsCellOffset = stateCursor.Offset;
            loadBits[i] = XPointerReference.FromRaw(
                stateCursor.ReadInt32(),
                XPointerResolutionMode.AliasCell,
                stateCursor.AddressAt(loadBitsCellOffset));
            stateCursor.ReadInt32();
        }

        foreach (XPointerReference loadBitsPointer in loadBits)
            ReadGfxStateBitsLoadBits(cursor, loadBitsPointer, context);
    }

    private static void ReadGfxStateBitsLoadBits(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        const int byteCount = 2 * sizeof(int);

        context.Diagnostics.Trace(
            $"        GfxStateBits.LoadBits raw=0x{pointer.Raw:X8} mode={pointer.ResolutionMode} source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");

        if (pointer.Type == PointerType.Null)
            return;

        if (pointer.Type == PointerType.Offset)
        {
            try
            {
                context.PointerReader.ValidateOffsetPointerRange(pointer, byteCount, "GfxStateBits.LoadBits");
            }
            catch (InvalidDataException)
            {
                context.Diagnostics.Trace(
                    $"      GfxStateBits raw loadBits=0x{pointer.Raw:X8} at {pointer.CellAddress} is not a materialized alias pointer.");
            }

            return;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress loadBitsAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            context.Blocks.Load(cursor, byteCount);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(loadBitsAddress));
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void ReadXStringPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative Material XString count {count}.");

        if (pointer.Type == PointerType.Null)
            return;

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

        foreach (XPointer<string> xstring in pointers)
            ReadXString(cursor, xstring, context);
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
