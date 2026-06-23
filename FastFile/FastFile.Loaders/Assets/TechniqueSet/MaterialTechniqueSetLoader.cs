using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Pointers;
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
    private const int VertexDeclSize = 0x1c;
    private const int VertexShaderSize = 0x0c;
    private const int PixelShaderSize = 0x18;
    private const int ShaderArgSize = 0x08;

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
            context.Blocks.AlignCurrent(4);
            return ReadTechniqueSet(cursor, context);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MaterialTechniqueSetAsset ReadTechniqueSet(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSetSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int namePointer = rootCursor.ReadInt32();
        var worldVertFormat = (MaterialWorldVertexFormat)rootCursor.ReadByte();
        byte[] unknown05 = rootCursor.ReadBytes(3);

        var techniquePointers = new int[TechniqueSlotCount];
        for (int i = 0; i < techniquePointers.Length; i++)
            techniquePointers[i] = rootCursor.ReadInt32();

        if (rootCursor.Offset != TechniqueSetSize)
            throw new InvalidDataException($"MaterialTechniqueSet consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSetSize:X}.");

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            string? name = ReadXString(cursor, namePointer, context);

            var slots = new MaterialTechniqueSlot[TechniqueSlotCount];
            for (int i = 0; i < techniquePointers.Length; i++)
            {
                XPointerReference techniquePointer = context.PointerReader.FromRaw(techniquePointers[i], XPointerOffsetMode.Direct);
                MaterialTechniqueAsset? technique = ReadTechniquePointer(cursor, techniquePointer, context);
                slots[i] = new MaterialTechniqueSlot(
                    i,
                    techniquePointer.AsPointer<MaterialTechniqueAsset>(),
                    technique);
            }

            return new MaterialTechniqueSetAsset
            {
                Offset = offset,
                NamePointer = context.PointerReader.FromRaw<string>(namePointer, XPointerResolutionMode.Direct),
                Name = name,
                WorldVertexFormat = worldVertFormat,
                Unknown05 = unknown05,
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
            return null;

        context.Blocks.AlignCurrent(4);
        return ReadTechnique(cursor, context);
    }

    private static MaterialTechniqueAsset ReadTechnique(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, TechniqueSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int namePointer = rootCursor.ReadInt32();
        ushort flags = rootCursor.ReadUInt16();
        ushort passCount = rootCursor.ReadUInt16();

        if (rootCursor.Offset != TechniqueSize)
            throw new InvalidDataException($"MaterialTechnique consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TechniqueSize:X}.");

        var passes = new MaterialPassAsset[passCount];
        for (int i = 0; i < passes.Length; i++)
            passes[i] = ReadPassRoot(cursor, context);

        for (int i = 0; i < passes.Length; i++)
            ReadPassChildren(cursor, passes[i], context);

        string? name = ReadXString(cursor, namePointer, context);

        return new MaterialTechniqueAsset
        {
            Offset = offset,
            NamePointer = context.PointerReader.FromRaw<string>(namePointer, XPointerResolutionMode.Direct),
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
        byte[] rootBytes = context.Blocks.Load(cursor, PassSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int vertexDecl = rootCursor.ReadInt32();
        int vertexShader = rootCursor.ReadInt32();
        int pixelShader = rootCursor.ReadInt32();
        byte perPrimArgCount = rootCursor.ReadByte();
        byte perObjArgCount = rootCursor.ReadByte();
        byte stableArgCount = rootCursor.ReadByte();
        byte customSamplerFlags = rootCursor.ReadByte();
        byte precompiledIndex = rootCursor.ReadByte();
        rootCursor.Skip(3);
        int args = rootCursor.ReadInt32();

        if (rootCursor.Offset != PassSize)
            throw new InvalidDataException($"MaterialPass consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PassSize:X}.");

        return new MaterialPassAsset
        {
            Offset = offset,
            VertexDeclPointer = context.PointerReader.FromRaw<MaterialVertexDeclarationAsset>(vertexDecl, XPointerResolutionMode.Direct),
            VertexShaderPointer = context.PointerReader.FromRaw<MaterialShaderAsset>(vertexShader, XPointerResolutionMode.AliasCell),
            PixelShaderPointer = context.PointerReader.FromRaw<MaterialShaderAsset>(pixelShader, XPointerResolutionMode.AliasCell),
            PerPrimArgCount = perPrimArgCount,
            PerObjArgCount = perObjArgCount,
            StableArgCount = stableArgCount,
            CustomSamplerFlags = customSamplerFlags,
            PrecompiledIndex = precompiledIndex,
            ArgsPointer = context.PointerReader.FromRaw<MaterialShaderArgumentAsset[]>(args, XPointerResolutionMode.Direct)
        };
    }

    private static void ReadPassChildren(
        FastFileCursor cursor,
        MaterialPassAsset pass,
        FastFileLoadContext context)
    {
        pass.VertexDeclBytes = ReadVertexDeclPointer(cursor, pass.VertexDeclPointer.Untyped, context);
        pass.VertexShader = ReadShaderPointer(cursor, pass.VertexShaderPointer.Untyped, MaterialShaderKind.Vertex, context);
        pass.PixelShader = ReadShaderPointer(cursor, pass.PixelShaderPointer.Untyped, MaterialShaderKind.Pixel, context);
        pass.Args = ReadShaderArgs(cursor, pass.ArgsPointer.Untyped, pass.PerPrimArgCount + pass.PerObjArgCount + pass.StableArgCount, context);
    }

    private static byte[]? ReadVertexDeclPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            return null;

        context.Blocks.AlignCurrent(4);
        return context.Blocks.Load(cursor, VertexDeclSize);
    }

    private static MaterialShaderAsset? ReadShaderPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        MaterialShaderKind kind,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            return null;

        context.Blocks.AlignCurrent(4);
        return kind == MaterialShaderKind.Vertex
            ? ReadVertexShader(cursor, context)
            : ReadPixelShader(cursor, context);
    }

    private static MaterialShaderAsset ReadVertexShader(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, VertexShaderSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int namePointer = rootCursor.ReadInt32();
        int dataPointer = rootCursor.ReadInt32();
        uint dataSize = rootCursor.ReadUInt32();

        if (rootCursor.Offset != VertexShaderSize)
            throw new InvalidDataException($"MaterialVertexShader consumed 0x{rootCursor.Offset:X} bytes instead of 0x{VertexShaderSize:X}.");

        string? name = ReadXString(cursor, namePointer, context);
        XPointerReference dataPointerRef = context.PointerReader.FromRaw(dataPointer, XPointerOffsetMode.AliasCell);
        byte[]? data = ReadShaderBytecode(cursor, dataPointerRef, dataSize, context);

        return new MaterialShaderAsset
        {
            Offset = offset,
            Kind = MaterialShaderKind.Vertex,
            NamePointer = context.PointerReader.FromRaw<string>(namePointer, XPointerResolutionMode.Direct),
            Name = name,
            DataPointer = dataPointerRef.AsPointer<MaterialShaderBytecode>(),
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
        byte[] rootBytes = context.Blocks.Load(cursor, PixelShaderSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int namePointer = rootCursor.ReadInt32();
        int dataPointer = rootCursor.ReadInt32();
        uint dataSize = rootCursor.ReadUInt32();
        byte[] unknown08 = rootCursor.ReadBytes(0x0c);

        if (rootCursor.Offset != PixelShaderSize)
            throw new InvalidDataException($"MaterialPixelShader consumed 0x{rootCursor.Offset:X} bytes instead of 0x{PixelShaderSize:X}.");

        string? name = ReadXString(cursor, namePointer, context);
        XPointerReference dataPointerRef = context.PointerReader.FromRaw(dataPointer, XPointerOffsetMode.AliasCell);
        byte[]? data = ReadShaderBytecode(cursor, dataPointerRef, dataSize, context);

        return new MaterialShaderAsset
        {
            Offset = offset,
            Kind = MaterialShaderKind.Pixel,
            NamePointer = context.PointerReader.FromRaw<string>(namePointer, XPointerResolutionMode.Direct),
            Name = name,
            DataPointer = dataPointerRef.AsPointer<MaterialShaderBytecode>(),
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

        if (!context.PointerReader.HasInlinePayload(pointer))
            return null;

        context.Blocks.AlignCurrent(16);
        return context.Blocks.Load(cursor, (int)dataSize);
    }

    private static IReadOnlyList<MaterialShaderArgumentAsset> ReadShaderArgs(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            return [];

        if (count < 0)
            throw new InvalidDataException($"Invalid negative shader arg count {count}.");

        context.Blocks.AlignCurrent(4);
        byte[] argBytes = context.Blocks.Load(cursor, checked(count * ShaderArgSize));
        var argCursor = new FastFileCursor(argBytes);
        var args = new MaterialShaderArgumentAsset[count];

        for (int i = 0; i < args.Length; i++)
        {
            int offset = cursor.Offset - argBytes.Length + i * ShaderArgSize;
            int argStart = argCursor.Offset;
            ushort type = argCursor.ReadUInt16();
            ushort dest = argCursor.ReadUInt16();
            int argumentRaw = argCursor.ReadInt32();

            if (argCursor.Offset - argStart != ShaderArgSize)
                throw new InvalidDataException($"MaterialShaderArgument consumed 0x{argCursor.Offset - argStart:X} bytes instead of 0x{ShaderArgSize:X}.");

            args[i] = new MaterialShaderArgumentAsset(offset, type, dest, argumentRaw, LiteralFloat4Bytes: null);
        }

        for (int i = 0; i < args.Length; i++)
        {
            byte[]? literal = null;
            XPointerReference argumentPointer = context.PointerReader.FromRaw(args[i].ArgumentRaw, XPointerOffsetMode.Direct);
            if (args[i].Type is 1 or 7 && context.PointerReader.HasInlinePayload(argumentPointer))
            {
                context.Blocks.AlignCurrent(16);
                literal = context.Blocks.Load(cursor, 0x10);
            }

            args[i] = args[i] with { LiteralFloat4Bytes = literal };
        }

        return args;
    }

    private static string? ReadXString(
        FastFileCursor cursor,
        int pointerRaw,
        FastFileLoadContext context)
    {
        XPointerReference pointer = context.PointerReader.FromRaw(pointerRaw, XPointerOffsetMode.Direct);
        return context.PointerReader.HasInlinePayload(pointer)
            ? context.Blocks.LoadCString(cursor)
            : null;
    }
}
