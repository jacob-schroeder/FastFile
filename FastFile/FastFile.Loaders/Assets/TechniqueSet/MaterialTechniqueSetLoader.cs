using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.TechniqueSet;

public sealed class MaterialTechniqueSetLoader
{
    private const int TechniqueSlotCount = 37;
    private const int TechniqueSetSize = 0x9c;
    private const int TechniqueSize = 0x08;
    private const int PassSize = 0x18;
    private const int VertexShaderSize = 0x0c;
    private const int PixelShaderSize = 0x18;
    private const int ShaderArgSize = 0x08;
    private const int LiteralFloat4Size = 0x10;

    public MaterialTechniqueSetAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level Techset pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            return ReadTechniqueSet(cursor, pointer, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialTechniqueSetAsset ReadTechniqueSet(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSetSize, out XBlockAddress rootAddress);
        if (rootAddress != targetAddress)
            throw new InvalidDataException($"MaterialTechniqueSet pointer patched to {targetAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        var worldVertFormat = (MaterialWorldVertexFormat)rootCursor.ReadByte();
        rootCursor.Align(4);

        var techniquePointers = new XPointerReference[TechniqueSlotCount];
        for (int i = 0; i < techniquePointers.Length; i++)
            techniquePointers[i] = ReadDeferredCell(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != TechniqueSetSize)
            throw new InvalidDataException($"MaterialTechniqueSet consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSetSize:X}.");

        int inlineTechniqueCount = techniquePointers.Count(x => x.Raw == -1);
        int offsetTechniqueCount = techniquePointers.Count(x => x.Raw != 0 && x.Raw != -1 && x.Raw != -2);
        context.Diagnostics.Trace(
            $"  Techset root source=0x{offset:X} name=0x{namePointer.Raw:X8} worldVertFormat={worldVertFormat} " +
            $"techniques.inline={inlineTechniqueCount} techniques.offset={offsetTechniqueCount} techniques.nonNull={techniquePointers.Count(x => x.Raw != 0)} blocks={context.Blocks.DescribePositions()}");

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            string? name = ReadXString(cursor, namePointer, context);

            var slots = new MaterialTechniqueSlot[TechniqueSlotCount];
            for (int i = 0; i < techniquePointers.Length; i++)
            {
                XPointerReference techniquePointer = techniquePointers[i];
                MaterialTechniqueAsset? technique = ReadTechniquePointer(cursor, techniquePointer, context);
                slots[i] = new MaterialTechniqueSlot(
                    i,
                    techniquePointer.AsPointer<MaterialTechniqueAsset>(),
                    technique);
            }

            return new MaterialTechniqueSetAsset
            {
                Offset = offset,
                NamePointer = namePointer,
                Name = name,
                WorldVertexFormat = worldVertFormat,
                TechniqueSlots = slots
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialTechniqueAsset? ReadTechniquePointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialTechniqueAsset>(pointer, TechniqueSize, "MaterialTechnique");
            return null;
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        return ReadTechnique(cursor, context);
    }

    private static XPointerReference ReadDeferredCell(
        FastFileCursor cursor,
        XPointerResolutionMode resolutionMode)
    {
        int cellOffset = cursor.Offset;
        return XPointerReference.FromRaw(
            cursor.ReadInt32(),
            resolutionMode,
            cursor.AddressAt(cellOffset));
    }

    private static MaterialTechniqueAsset ReadTechnique(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        ushort flags = rootCursor.ReadUInt16();
        ushort passCount = rootCursor.ReadUInt16();

        if (rootCursor.Offset != TechniqueSize)
            throw new InvalidDataException($"MaterialTechnique consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSize:X}.");

        context.Diagnostics.Trace(
            $"    MaterialTechnique root source=0x{offset:X} name=0x{namePointer.Raw:X8} flags=0x{flags:X4} passCount={passCount} blocks={context.Blocks.DescribePositions()}");

        var passes = new MaterialPassAsset[passCount];
        for (int i = 0; i < passes.Length; i++)
            passes[i] = ReadPassRoot(cursor, context);

        for (int i = 0; i < passes.Length; i++)
            ReadPassChildren(cursor, passes[i], context);

        string? name = ReadXString(cursor, namePointer, context);

        return new MaterialTechniqueAsset
        {
            Offset = offset,
            NamePointer = namePointer,
            Name = name,
            Flags = flags,
            PassCount = passCount,
            Passes = passes
        };
    }

    private static MaterialPassAsset ReadPassRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, PassSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<MaterialVertexDeclarationAsset> vertexDecl = context.PointerReader.ReadPointer<MaterialVertexDeclarationAsset>(rootCursor, XPointerResolutionMode.Direct);
        XPointer<MaterialShaderAsset> vertexShader = context.PointerReader.ReadPointer<MaterialShaderAsset>(rootCursor, XPointerResolutionMode.AliasCell);
        XPointer<MaterialShaderAsset> pixelShader = context.PointerReader.ReadPointer<MaterialShaderAsset>(rootCursor, XPointerResolutionMode.AliasCell);
        byte perPrimArgCount = rootCursor.ReadByte();
        byte perObjArgCount = rootCursor.ReadByte();
        byte stableArgCount = rootCursor.ReadByte();
        byte customSamplerFlags = rootCursor.ReadByte();
        byte precompiledIndex = rootCursor.ReadByte();
        rootCursor.Skip(3);
        XPointer<MaterialShaderArgumentAsset[]> args = context.PointerReader.ReadPointer<MaterialShaderArgumentAsset[]>(rootCursor, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != PassSize)
            throw new InvalidDataException($"MaterialPass consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PassSize:X}.");

        context.Diagnostics.Trace(
            $"      MaterialPass root source=0x{offset:X} vd=0x{vertexDecl.Raw:X8} vs=0x{vertexShader.Raw:X8} ps=0x{pixelShader.Raw:X8} " +
            $"args={perPrimArgCount}+{perObjArgCount}+{stableArgCount} argsPtr=0x{args.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        return new MaterialPassAsset
        {
            Offset = offset,
            VertexDeclPointer = vertexDecl,
            VertexShaderPointer = vertexShader,
            PixelShaderPointer = pixelShader,
            PerPrimArgCount = perPrimArgCount,
            PerObjArgCount = perObjArgCount,
            StableArgCount = stableArgCount,
            CustomSamplerFlags = customSamplerFlags,
            PrecompiledIndex = precompiledIndex,
            ArgsPointer = args
        };
    }

    private static void ReadPassChildren(
        FastFileCursor cursor,
        MaterialPassAsset pass,
        FastFileLoadContext context)
    {
        pass.VertexDeclaration = ReadVertexDeclPointer(cursor, pass.VertexDeclPointer.Untyped, context);
        pass.VertexShader = ReadShaderPointer(cursor, pass.VertexShaderPointer.Untyped, MaterialShaderKind.Vertex, context);
        pass.PixelShader = ReadShaderPointer(cursor, pass.PixelShaderPointer.Untyped, MaterialShaderKind.Pixel, context);
        pass.Args = ReadShaderArgs(cursor, pass.ArgsPointer.Untyped, pass.PerPrimArgCount + pass.PerObjArgCount + pass.StableArgCount, context);
    }

    private static MaterialVertexDeclarationAsset? ReadVertexDeclPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialVertexDeclarationAsset>(
                pointer,
                MaterialVertexDeclarationAsset.SerializedSize,
                "MaterialVertexDeclaration");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, MaterialVertexDeclarationAsset.SerializedSize);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        byte streamCount = rootCursor.ReadByte();
        byte hasOptionalSource = rootCursor.ReadByte();
        var routing = new MaterialVertexStreamRouting[MaterialVertexDeclarationAsset.RoutingCount];
        for (int i = 0; i < routing.Length; i++)
            routing[i] = new MaterialVertexStreamRouting(rootCursor.ReadByte(), rootCursor.ReadByte());

        if (rootCursor.Offset != MaterialVertexDeclarationAsset.SerializedSize)
            throw new InvalidDataException($"MaterialVertexDeclaration consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MaterialVertexDeclarationAsset.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"        MaterialVertexDeclaration root sourceEnd=0x{cursor.Offset:X} streamCount={streamCount} " +
            $"hasOptionalSource={hasOptionalSource} target={rootAddress} blocks={context.Blocks.DescribePositions()}");

        return new MaterialVertexDeclarationAsset
        {
            StreamCount = streamCount,
            HasOptionalSource = hasOptionalSource,
            Routing = routing
        };
    }

    private static MaterialShaderAsset? ReadShaderPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        MaterialShaderKind kind,
        FastFileLoadContext context)
    {
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            if (pointer.Type == PointerType.Null)
                return null;

            if (pointer.Type == PointerType.Offset)
            {
                int rootSize = kind == MaterialShaderKind.Vertex ? VertexShaderSize : PixelShaderSize;
                context.PointerReader.ValidateOffsetPointerRange<MaterialShaderAsset>(pointer, rootSize, $"Material{kind}Shader");
                return null;
            }

            if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
                return null;

            XBlockAddress? insertCell = pointer.Type == PointerType.Insert
                ? context.Blocks.AllocateInsertPointerCell()
                : null;

            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            MaterialShaderAsset shader = kind == MaterialShaderKind.Vertex
                ? ReadVertexShader(cursor, context)
                : ReadPixelShader(cursor, context);

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return shader;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialShaderAsset ReadVertexShader(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, VertexShaderSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        XPointerReference dataPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);
        uint dataSize = rootCursor.ReadUInt32();

        if (rootCursor.Offset != VertexShaderSize)
            throw new InvalidDataException($"MaterialVertexShader consumed 0x{rootCursor.Offset:X} bytes instead of 0x{VertexShaderSize:X}.");

        context.Diagnostics.Trace(
            $"        MaterialVertexShader root source=0x{offset:X} name=0x{namePointer.Raw:X8} data=0x{dataPointer.Raw:X8} " +
            $"dataSize=0x{dataSize:X} root={rootAddress} blocks={context.Blocks.DescribePositions()}");

        string? name;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = ReadXString(cursor, namePointer, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        byte[]? data = ReadShaderBytecode(cursor, dataPointer, dataSize, context);

        return new MaterialShaderAsset
        {
            Offset = offset,
            Kind = MaterialShaderKind.Vertex,
            NamePointer = namePointer,
            Name = name,
            DataPointer = dataPointer.AsPointer<MaterialShaderBytecode>(),
            DataSize = dataSize,
            ProgramBytes = [],
            Data = data
        };
    }

    private static MaterialShaderAsset ReadPixelShader(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, PixelShaderSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        XPointerReference dataPointer = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.AliasCell);
        uint dataSize = rootCursor.ReadUInt32();
        byte[] unknown08 = rootCursor.ReadBytes(0x0c);

        if (rootCursor.Offset != PixelShaderSize)
            throw new InvalidDataException($"MaterialPixelShader consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PixelShaderSize:X}.");

        context.Diagnostics.Trace(
            $"        MaterialPixelShader root source=0x{offset:X} name=0x{namePointer.Raw:X8} data=0x{dataPointer.Raw:X8} " +
            $"dataSize=0x{dataSize:X} root={rootAddress} blocks={context.Blocks.DescribePositions()}");

        string? name;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = ReadXString(cursor, namePointer, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        byte[]? data = ReadShaderBytecode(cursor, dataPointer, dataSize, context);

        return new MaterialShaderAsset
        {
            Offset = offset,
            Kind = MaterialShaderKind.Pixel,
            NamePointer = namePointer,
            Name = name,
            DataPointer = dataPointer.AsPointer<MaterialShaderBytecode>(),
            DataSize = dataSize,
            ProgramBytes = unknown08,
            Data = data
        };
    }

    private static byte[]? ReadShaderBytecode(
        FastFileCursor cursor,
        XPointerReference pointer,
        uint dataSize,
        FastFileLoadContext context)
    {
        if (dataSize > int.MaxValue)
            throw new InvalidDataException($"Shader bytecode size 0x{dataSize:X} does not fit in this reader.");

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialShaderBytecode>(pointer, (int)dataSize, "MaterialShaderBytecode");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress dataAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 16);
        byte[] data = context.Blocks.Load(cursor, (int)dataSize);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(dataAddress));

        context.Diagnostics.Trace(
            $"          MaterialShader bytecode sourceEnd=0x{cursor.Offset:X} ptr=0x{pointer.Raw:X8} target={dataAddress} " +
            $"bytes=0x{dataSize:X} blocks={context.Blocks.DescribePositions()}");

        return data;
    }

    private static IReadOnlyList<MaterialShaderArgumentAsset> ReadShaderArgs(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative shader arg count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialShaderArgumentAsset[]>(pointer, checked(count * ShaderArgSize), "MaterialShaderArgument[]");
            return [];
        }

        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] argBytes = context.Blocks.Load(cursor, checked(count * ShaderArgSize), out XBlockAddress argAddress);
        var argCursor = new FastFileCursor(argBytes, argAddress);
        var args = new MaterialShaderArgumentAsset[count];
        var argumentPointers = new XPointerReference[count];

        for (int i = 0; i < args.Length; i++)
        {
            int offset = cursor.Offset - argBytes.Length + i * ShaderArgSize;
            int argStart = argCursor.Offset;
            var type = (MaterialShaderArgumentType)argCursor.ReadUInt16();
            ushort dest = argCursor.ReadUInt16();
            int valueCellOffset = argCursor.Offset;
            XPointerReference argumentPointer = XPointerReference.FromRaw(
                argCursor.ReadInt32(),
                XPointerResolutionMode.Direct,
                argCursor.AddressAt(valueCellOffset));

            if (argCursor.Offset - argStart != ShaderArgSize)
                throw new InvalidDataException($"MaterialShaderArgument consumed 0x{argCursor.Offset - argStart:X} bytes instead of 0x{ShaderArgSize:X}.");

            argumentPointers[i] = argumentPointer;
            args[i] = new MaterialShaderArgumentAsset(offset, type, dest, argumentPointer.Raw, LiteralConstant: null);
        }

        for (int i = 0; i < args.Length; i++)
        {
            MaterialShaderLiteralConstant? literal = null;
            XPointerReference argumentPointer = argumentPointers[i];
            if (args[i].Type is MaterialShaderArgumentType.LiteralVertexConst or MaterialShaderArgumentType.LiteralPixelConst)
                literal = ReadLiteralFloat4Pointer(cursor, argumentPointer, context);

            args[i] = args[i] with { LiteralConstant = literal };
        }

        return args;
    }

    private static MaterialShaderLiteralConstant? ReadLiteralFloat4Pointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.PackedAddress is { } packedAddress)
        {
            context.PointerReader.ValidateOffsetPointerRange<MaterialShaderLiteralConstant>(
                pointer,
                LiteralFloat4Size,
                "MaterialShaderLiteralConstant");
            return ReadLiteralFloat4(context.Blocks.ReadBytes(packedAddress, LiteralFloat4Size), packedAddress);
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            return null;

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        XBlockAddress literalAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 16);
        byte[] literalBytes = context.Blocks.Load(cursor, LiteralFloat4Size);
        if (insertCell is { } cell)
            context.Blocks.WriteInt32(cell, XPointerCodec.Encode(literalAddress));

        return ReadLiteralFloat4(literalBytes, literalAddress);
    }

    private static MaterialShaderLiteralConstant ReadLiteralFloat4(byte[] literalBytes, XBlockAddress literalAddress)
    {
        var literalCursor = new FastFileCursor(literalBytes, literalAddress);
        var literal = new MaterialShaderLiteralConstant(
            ReadSingle(literalCursor),
            ReadSingle(literalCursor),
            ReadSingle(literalCursor),
            ReadSingle(literalCursor));

        if (literalCursor.Offset != LiteralFloat4Size)
            throw new InvalidDataException($"MaterialShaderLiteralConstant consumed 0x{literalCursor.Offset:X} bytes instead of 0x{LiteralFloat4Size:X}.");

        return literal;
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

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }
}
