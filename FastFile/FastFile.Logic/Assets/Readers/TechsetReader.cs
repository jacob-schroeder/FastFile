using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class TechsetReader
{
    private const int MaxMaterialTechniquePassCount = 64;
    private const int MaxMaterialPassArgCount = 128;
    private const int MaxMaterialShaderProgramBytes = 2 * 1024 * 1024;

    private static readonly bool TraceTechset =
        Environment.GetEnvironmentVariable("FASTFILE_TRACE_TECHSET") is { Length: > 0 } value
        && value != "0";
    private static readonly int TraceShaderProgramLimit = GetTraceLimit("FASTFILE_TRACE_TECHSET_SHADER_LIMIT", 2048);
    private static readonly int TraceTechniqueLimit = GetTraceLimit("FASTFILE_TRACE_TECHSET_TECHNIQUE_LIMIT", 256);
    private static readonly int TracePassLimit = GetTraceLimit("FASTFILE_TRACE_TECHSET_PASS_LIMIT", 512);

    private static int _traceShaderProgramCount;
    private static int _traceTechniqueCount;
    private static int _tracePassCount;

    public static MaterialTechniqueSet Read(ref XFileReadContext context)
    {
        var asset = new MaterialTechniqueSet
        {
            Offset = context.Position,
            NamePtr = ReadStringPointerInBlock(ref context, XFILE_BLOCK.LARGE),
            WorldVertexFormat = (MaterialWorldVertexFormat)context.ReadByte(),
            HasBeenUploaded = context.ReadByte() != 0,
            Unused = context.ReadBytes(2),
        };
        XFileReadValidator.ValidateEnum(
            ref context,
            "MaterialTechniqueSet.WorldVertexFormat",
            asset.WorldVertexFormat,
            "EBOOT 0x00107e80 reads this byte from the Techset root before the 37 technique pointer cells.");

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);

        for (var i = 0; i < asset.Techniques.Length; i++)
        {
            asset.Techniques[i] = context.ReadDirectPointer<MaterialTechnique>($"Techset.Techniques[{i}]");
            context.ResolvePointerInBlock(
                asset.Techniques[i],
                XFILE_BLOCK.LARGE,
                ReadMaterialTechniquePointerValue);
        }

        return asset;
    }

    public static MaterialPixelShader ReadPixelShader(ref XFileReadContext context)
    {
        var asset = new MaterialPixelShader
        {
            Offset = context.Position,
            NamePtr = ReadStringPointerInBlock(ref context, XFILE_BLOCK.LARGE),
            Program = ReadPixelShaderProgram(ref context),
        };

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);
        ResolveProgramData(ref context, asset.Program.Data, asset.Program.DataSize);
        return asset;
    }

    public static MaterialVertexShader ReadVertexShader(ref XFileReadContext context)
    {
        var asset = new MaterialVertexShader
        {
            Offset = context.Position,
            NamePtr = ReadStringPointerInBlock(ref context, XFILE_BLOCK.LARGE),
            Program = ReadVertexShaderProgram(ref context),
        };

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);
        ResolveProgramData(ref context, asset.Program.Data, asset.Program.DataSize);
        return asset;
    }

    private static ZonePointer<string> ReadStringPointerInBlock(
        ref XFileReadContext context,
        XFILE_BLOCK block)
    {
        var pointer = GenericReader.ReadStringPointer(ref context, resolve: false);
        return pointer;
    }

    private static void ReadMaterialTechniquePointerValue(
        ref XFileReadContext context,
        ZonePointer<MaterialTechnique> pointer)
    {
        var value = context.ReadPointerValue(pointer, ReadMaterialTechnique);
        pointer.SetResult(value);
    }

    private static MaterialTechnique ReadMaterialTechnique(ref XFileReadContext context)
    {
        var technique = new MaterialTechnique
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            Flags = context.ReadUInt16(),
            PassCount = context.ReadUInt16(),
        };
        XFileReadValidator.ValidateCount(
            ref context,
            "MaterialTechnique.PassCount",
            technique.PassCount,
            0,
            MaxMaterialTechniquePassCount,
            "EBOOT 0x00107c88 reads passCount from +0x06 and then helper 0x00107bf8 consumes passCount * 0x18 MaterialPass rows; a huge value indicates the technique root is desynced.");

        TraceTechnique(ref context, technique);

        technique.Passes = new MaterialPass[technique.PassCount];
        for (var i = 0; i < technique.Passes.Length; i++)
            technique.Passes[i] = ReadMaterialPass(ref context, i);

        context.ResolvePointerInBlock(
            technique.NamePtr,
            XFILE_BLOCK.LARGE,
            GenericReader.ReadStringPointerValue);

        return technique;
    }

    private static MaterialPass ReadMaterialPass(ref XFileReadContext context, int index)
    {
        var pass = new MaterialPass
        {
            Offset = context.Position,
            VertexDecl = context.ReadDirectPointer<MaterialVertexDeclaration>($"MaterialPass[{index}].VertexDecl"),
            VertexShader = context.ReadAliasPointer<MaterialVertexShader>($"MaterialPass[{index}].VertexShader"),
            PixelShader = context.ReadAliasPointer<MaterialPixelShader>($"MaterialPass[{index}].PixelShader"),
            PerPrimArgCount = context.ReadByte(),
            PerObjArgCount = context.ReadByte(),
            StableArgCount = context.ReadByte(),
            CustomSamplerFlags = context.ReadByte(),
            PrecompiledIndex = context.ReadByte(),
            Padding = context.ReadBytes(3),
            Args = context.ReadDirectPointer<MaterialShaderArgument[]>($"MaterialPass[{index}].Args"),
        };
        XFileReadValidator.ValidateCount(
            ref context,
            $"MaterialPass[{index}].ArgCount",
            pass.ArgCount,
            0,
            MaxMaterialPassArgCount,
            "EBOOT 0x00107a80 derives arg count from bytes +0x0c/+0x0d/+0x0e and helper 0x000f8280 consumes argCount * 8 rows.");

        TracePass(ref context, index, pass);

        context.ResolvePointerInBlock(
            pass.VertexDecl,
            XFILE_BLOCK.LARGE,
            ReadVertexDeclarationPointerValue);
        context.ResolvePointerInBlock(
            pass.VertexShader,
            XFILE_BLOCK.TEMP,
            ReadVertexShaderPointerValue);
        context.ResolvePointerInBlock(
            pass.PixelShader,
            XFILE_BLOCK.TEMP,
            ReadPixelShaderPointerValue);

        var argCount = pass.ArgCount;
        context.ResolvePointerInBlock(
            pass.Args,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<MaterialShaderArgument[]> pointer) =>
            {
                var values = new MaterialShaderArgument[Math.Max(0, argCount)];
                for (var i = 0; i < values.Length; i++)
                    values[i] = ReadMaterialShaderArgument(ref pointerContext, i);
                pointer.SetResult(values);
            });

        return pass;
    }

    private static void ReadVertexDeclarationPointerValue(
        ref XFileReadContext context,
        ZonePointer<MaterialVertexDeclaration> pointer)
    {
        var value = context.ReadPointerValue(pointer, ReadVertexDeclaration);
        pointer.SetResult(value);
    }

    private static MaterialVertexDeclaration ReadVertexDeclaration(ref XFileReadContext context)
    {
        return new MaterialVertexDeclaration
        {
            Offset = context.Position,
            Raw = context.ReadBytes(0x1C),
        };
    }

    private static void ReadVertexShaderPointerValue(
        ref XFileReadContext context,
        ZonePointer<MaterialVertexShader> pointer)
    {
        var value = context.ReadPointerValue(pointer, ReadVertexShader);
        pointer.SetResult(value);
    }

    private static void ReadPixelShaderPointerValue(
        ref XFileReadContext context,
        ZonePointer<MaterialPixelShader> pointer)
    {
        var value = context.ReadPointerValue(pointer, ReadPixelShader);
        pointer.SetResult(value);
    }

    private static MaterialShaderArgument ReadMaterialShaderArgument(
        ref XFileReadContext context,
        int index)
    {
        var rawType = context.ReadUInt16();
        XFileReadValidator.ValidateShaderArgumentType(
            ref context,
            $"MaterialShaderArgument[{index}].Type",
            rawType,
            "EBOOT reads 8-byte MaterialShaderArgument rows; IW4 PS3 uses the known 0..7 argument type range.");
        var type = (MaterialShaderArgumentType)rawType;
        var dest = context.ReadUInt16();
        var raw = context.ReadInt32();
        var value = new MaterialShaderArgument
        {
            Type = type,
            Dest = dest,
            Argument = new MaterialArgumentDef
            {
                Raw = raw,
                CodeSampler = unchecked((uint)raw),
                NameHash = unchecked((uint)raw),
                CodeConst = new MaterialArgumentCodeConst
                {
                    Index = unchecked((ushort)((raw >> 16) & 0xFFFF)),
                    FirstRow = unchecked((byte)((raw >> 8) & 0xFF)),
                    RowCount = unchecked((byte)(raw & 0xFF)),
                },
            },
        };

        if (type is MaterialShaderArgumentType.MTL_ARG_LITERAL_VERTEX_CONST
            or MaterialShaderArgumentType.MTL_ARG_LITERAL_PIXEL_CONST)
        {
            value.Argument.LiteralConst = context.CreatePointer<float[]>(
                raw,
                resolutionKind: PointerResolutionKind.Direct,
                fieldPath: $"MaterialShaderArgument[{index}].LiteralConst");
            context.ResolvePointerInBlock(
                value.Argument.LiteralConst,
                XFILE_BLOCK.LARGE,
                ReadLiteralConstPointerValue);
        }

        return value;
    }

    private static void ReadLiteralConstPointerValue(
        ref XFileReadContext context,
        ZonePointer<float[]> pointer)
    {
        var values = new float[4];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();
        pointer.SetResult(values);
    }

    private static MaterialPixelShaderProgram ReadPixelShaderProgram(ref XFileReadContext context)
    {
        var program = new MaterialPixelShaderProgram
        {
            Offset = context.Position,
            Data = context.ReadDirectPointer<byte[]>("MaterialPixelShader.Program.Data"),
            DataSize = context.ReadInt32(),
            RootSuffix = context.ReadBytes(0x0C),
        };
        XFileReadValidator.ValidateCount(
            ref context,
            "MaterialPixelShader.Program.DataSize",
            program.DataSize,
            0,
            MaxMaterialShaderProgramBytes,
            "EBOOT 0x000faff8 passes this count to raw byte helper 0x000e8d08 after 4-byte stream alignment.");
        TraceShaderProgram("pixel", program.Offset, program.Data.Raw, program.DataSize);
        return program;
    }

    private static MaterialVertexShaderProgram ReadVertexShaderProgram(ref XFileReadContext context)
    {
        var program = new MaterialVertexShaderProgram
        {
            Offset = context.Position,
            Data = context.ReadDirectPointer<byte[]>("MaterialVertexShader.Program.Data"),
            DataSize = context.ReadInt32(),
        };
        XFileReadValidator.ValidateCount(
            ref context,
            "MaterialVertexShader.Program.DataSize",
            program.DataSize,
            0,
            MaxMaterialShaderProgramBytes,
            "EBOOT 0x000fb368 passes this count to raw byte helper 0x000e8d08 after 4-byte stream alignment.");
        TraceShaderProgram("vertex", program.Offset, program.Data.Raw, program.DataSize);
        return program;
    }

    private static void ResolveProgramData(
        ref XFileReadContext context,
        ZonePointer<byte[]> pointer,
        int size)
    {
        context.ResolvePointerAlignedInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            XFileStreamAlignment.Four,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> p) =>
            {
                p.SetResult(pointerContext.ReadBytes(Math.Max(0, size)));
            });
    }

    private static void TraceShaderProgram(
        string kind,
        int offset,
        int dataRaw,
        int dataSize)
    {
        if (!TraceTechset)
            return;

        var count = Interlocked.Increment(ref _traceShaderProgramCount);
        if (count > TraceShaderProgramLimit)
            return;

        Console.Error.WriteLine(
            $"[techset-trace] {kind} program[{count:D4}] off=0x{offset:X8} dataRaw=0x{dataRaw:X8} dataSize=0x{dataSize:X8} ({dataSize})");
    }

    private static void TraceTechnique(ref XFileReadContext context, MaterialTechnique technique)
    {
        if (!TraceTechset)
            return;

        var count = Interlocked.Increment(ref _traceTechniqueCount);
        if (count > TraceTechniqueLimit)
            return;

        Console.Error.WriteLine(
            $"[techset-trace] technique[{count:D3}] asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"off=0x{technique.Offset:X8} nameRaw=0x{technique.NamePtr.Raw:X8} "
            + $"flags=0x{technique.Flags:X4} passes={technique.PassCount}");
    }

    private static void TracePass(ref XFileReadContext context, int index, MaterialPass pass)
    {
        if (!TraceTechset)
            return;

        var count = Interlocked.Increment(ref _tracePassCount);
        if (count > TracePassLimit)
            return;

        Console.Error.WriteLine(
            $"[techset-trace] pass[{count:D4}] asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"techPass={index} off=0x{pass.Offset:X8} "
            + $"vd=0x{pass.VertexDecl.Raw:X8} vs=0x{pass.VertexShader.Raw:X8} ps=0x{pass.PixelShader.Raw:X8} "
            + $"args=0x{pass.Args.Raw:X8} counts={pass.PerPrimArgCount}/{pass.PerObjArgCount}/{pass.StableArgCount} "
            + $"argTotal={pass.ArgCount} pre={pass.PrecompiledIndex}");
    }

    private static int GetTraceLimit(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value >= 0
            ? value
            : fallback;
    }
}
