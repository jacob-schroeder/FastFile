using System.Buffers.Binary;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using FastFile.Logic.Zone;

namespace FastFile.Logic.Assets.Readers;

public sealed class MaterialAssetReader : XAssetReadHandler
{
    private sealed class FixedCountOwner
    {
        public required int Count { get; init; }
    }

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case MaterialTechniqueSet techset:
                Load_MaterialTechniqueSet(techset, context);
                return true;

            case MaterialTechnique technique:
                Load_MaterialTechnique(technique, context);
                return true;

            case MaterialPass pass:
                Load_MaterialPass(pass, context);
                return true;

            case MaterialVertexShader vertexShader:
                Load_MaterialVertexShader(vertexShader, context);
                return true;

            case MaterialPixelShader pixelShader:
                Load_MaterialPixelShader(pixelShader, context);
                return true;

            case MaterialShaderArgument argument:
                Load_MaterialShaderArgument(argument, context);
                return true;

            default:
                return false;
        }
    }

    public override bool TryResolvePointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case MaterialTextureDef texture:
                ResolveMaterialTextureDef(texture, context);
                return true;

            case GfxImage image:
                ResolveGfxImagePointers(image, context);
                return true;

            default:
                return false;
        }
    }

    // PS3 0x107e80 / Xbox Load_MaterialTechniqueSet
    private static void Load_MaterialTechniqueSet(
        MaterialTechniqueSet techniqueSet,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(techniqueSet, nameof(MaterialTechniqueSet.NamePtr));
        Load_MaterialTechniquePtrArray(techniqueSet, context);
    }

    // PS3 0x107df8 -> 0x107cf0 / Xbox Load_MaterialTechniquePtrArray / Ptr
    private static void Load_MaterialTechniquePtrArray(
        MaterialTechniqueSet techniqueSet,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(techniqueSet, nameof(MaterialTechniqueSet.Techniques));
    }

    // PS3 0x107c88 / Xbox Load_MaterialTechnique
    private static void Load_MaterialTechnique(
        MaterialTechnique technique,
        IXAssetReaderContext context)
    {
        // EBOOT 0x107c88 consumes the pass array before resolving the technique name XString.
        var passes = new XPointer<MaterialPass[]>
        {
            Raw = -1,
            Kind = PointerKind.Inline,
            ResolutionKind = PointerResolutionKind.CurrentStream
        };

        context.ResolvePointerValue(
            passes,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.CurrentStream,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(MaterialTechnique.PassCount)
            },
            technique);

        technique.Passes = passes.Value ?? [];
        context.ResolvePointerProperty(technique, nameof(MaterialTechnique.NamePtr));
    }

    // PS3 0x107a80 / Xbox Load_MaterialPass
    private static void Load_MaterialPass(
        MaterialPass pass,
        IXAssetReaderContext context)
    {
        Load_MaterialVertexDeclarationPtr(pass, context);
        Load_MaterialVertexShaderPtr(pass, context);
        Load_MaterialPixelShaderPtr(pass, context);
        Load_MaterialShaderArgumentArray(pass, context);
    }

    // PS3 pass child +0x00
    private static void Load_MaterialVertexDeclarationPtr(
        MaterialPass pass,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(pass, nameof(MaterialPass.VertexDecl));
    }

    // PS3 0x107968 / Xbox Load_MaterialVertexShaderPtr
    private static void Load_MaterialVertexShaderPtr(
        MaterialPass pass,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(pass, nameof(MaterialPass.VertexShader));
    }

    // PS3 0x1075e8 / Xbox Load_MaterialPixelShaderPtr
    private static void Load_MaterialPixelShaderPtr(
        MaterialPass pass,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(pass, nameof(MaterialPass.PixelShader));
    }

    // PS3 0xf8280 / Xbox Load_MaterialShaderArgumentArray
    private static void Load_MaterialShaderArgumentArray(
        MaterialPass pass,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(pass, nameof(MaterialPass.Args));
    }

    // PS3 0x1078f8 / Xbox Load_MaterialVertexShader
    private static void Load_MaterialVertexShader(
        MaterialVertexShader shader,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(shader, nameof(MaterialVertexShader.NamePtr));
        Load_MaterialVertexShaderProgram(shader.Program, context);
    }

    // PS3 0xfb498 / Xbox Load_MaterialVertexShaderProgram
    private static void Load_MaterialVertexShaderProgram(
        MaterialVertexShaderProgram program,
        IXAssetReaderContext context)
    {
        Load_GfxVertexShaderLoadDef(program, context);
    }

    // PS3 0xfb368 / Xbox Load_GfxVertexShaderLoadDef
    private static void Load_GfxVertexShaderLoadDef(
        MaterialVertexShaderProgram program,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(program, nameof(MaterialVertexShaderProgram.Data));
    }

    // PS3 0x107578 / Xbox Load_MaterialPixelShader
    private static void Load_MaterialPixelShader(
        MaterialPixelShader shader,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(shader, nameof(MaterialPixelShader.NamePtr));
        Load_MaterialPixelShaderProgram(shader.Program, context);
    }

    // PS3 0xfb128 / Xbox Load_MaterialPixelShaderProgram
    private static void Load_MaterialPixelShaderProgram(
        MaterialPixelShaderProgram program,
        IXAssetReaderContext context)
    {
        Load_GfxPixelShaderLoadDef(program, context);
    }

    // PS3 0xfaff8 / Xbox Load_GfxPixelShaderLoadDef
    private static void Load_GfxPixelShaderLoadDef(
        MaterialPixelShaderProgram program,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(program, nameof(MaterialPixelShaderProgram.Data));
    }

    // PS3 0xf81c8 / Xbox Load_MaterialShaderArgument
    private static void Load_MaterialShaderArgument(
        MaterialShaderArgument argument,
        IXAssetReaderContext context)
    {
        Load_MaterialArgumentDef(argument, context);
    }

    // PS3 0xf80a0 / Xbox Load_MaterialArgumentDef family
    private static void Load_MaterialArgumentDef(
        MaterialShaderArgument argument,
        IXAssetReaderContext context)
    {
        var def = argument.Argument;
        var raw = unchecked((uint)def.Raw);

        switch (argument.Type)
        {
            case MaterialShaderArgumentType.MTL_ARG_LITERAL_VERTEX_CONST:
            case MaterialShaderArgumentType.MTL_ARG_LITERAL_PIXEL_CONST:
                def.LiteralConst = Load_LiteralFloat4(def.Raw, context);
                break;

            case MaterialShaderArgumentType.MTL_ARG_CODE_VERTEX_CONST:
            case MaterialShaderArgumentType.MTL_ARG_CODE_PIXEL_CONST:
                Load_MaterialArgumentCodeConst(def, raw);
                break;

            case MaterialShaderArgumentType.MTL_ARG_CODE_PIXEL_SAMPLER:
                def.CodeSampler = raw;
                break;

            case MaterialShaderArgumentType.MTL_ARG_MATERIAL_VERTEX_CONST:
            case MaterialShaderArgumentType.MTL_ARG_MATERIAL_PIXEL_SAMPLER:
            case MaterialShaderArgumentType.MTL_ARG_MATERIAL_PIXEL_CONST:
                def.NameHash = raw;
                break;
        }
    }

    // PS3 0xec970 / Xbox Load_MaterialArgumentCodeConst
    private static void Load_MaterialArgumentCodeConst(
        MaterialArgumentDef def,
        uint raw)
    {
        def.CodeConst = new MaterialArgumentCodeConst
        {
            Index = (ushort)(raw >> 16),
            FirstRow = (byte)(raw >> 8),
            RowCount = (byte)raw
        };
    }

    // PS3 0xec610 with count=4, inline align mask 0x0f
    private static XPointer<float[]> Load_LiteralFloat4(
        int raw,
        IXAssetReaderContext context)
    {
        var bytePointer = XPointerCodec.CreatePointer<byte[]>(
            raw,
            PointerResolutionKind.Direct);
        var owner = new FixedCountOwner { Count = 16 };

        context.ResolvePointerValue(
            bytePointer,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                UseCurrentStream = true,
                Alignment = 16,
                CountMember = nameof(FixedCountOwner.Count)
            },
            owner);

        var bytes = bytePointer.Value ?? [];
        if (bytes.Length != 16)
        {
            throw new InvalidDataException(
                $"Material shader literal expected 16 bytes but got {bytes.Length}.");
        }

        var values = new float[4];
        for (var i = 0; i < values.Length; i++)
            values[i] = BinaryPrimitives.ReadSingleBigEndian(bytes.AsSpan(i * 4, 4));

        return new XPointer<float[]>
        {
            Raw = raw,
            Kind = bytePointer.Kind,
            ResolutionKind = PointerResolutionKind.Direct,
            PatchAddress = bytePointer.PatchAddress,
            Address = bytePointer.Address,
            Value = values
        };
    }

    private static void ResolveMaterialTextureDef(
        MaterialTextureDef texture,
        IXAssetReaderContext context)
    {
        var dataPtr = texture.Info?.DataPtr;
        if (dataPtr is null || dataPtr.IsNull)
            return;

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
            throw new NotSupportedException("Water material texture payloads are not implemented yet.");

        texture.Info!.Image = context.ReinterpretPointer<GfxImage>(dataPtr, PointerResolutionKind.Direct);
        context.ResolvePointerValue(
            texture.Info.Image,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.Object,
                PayloadBlock = XFILE_BLOCK.TEMP
            },
            texture.Info);
    }

    private static void ResolveGfxImagePointers(
        GfxImage image,
        IXAssetReaderContext context)
    {
        PopulateGfxImageRootFields(image);

        // EBOOT 0x1084f0 resolves the image name before the load-def pointer.
        context.MaterializeCStringPointer(image.NamePtr);

        if (image.LoadDef.IsNull)
            return;

        var loadDef = new GfxImageLoadDef
        {
            LevelCount = image.EbootRootPrefix[0x01],
            Flags = ReadInt32(image.EbootRootPrefix, 0x04),
            Format = image.EbootRootPrefix[0x00] & 0x9F,
            ResourceSize = GetGfxImageResourceSize(image)
        };

        var data = new XPointer<byte[]>
        {
            Raw = image.LoadDef.Raw,
            Kind = image.LoadDef.Kind,
            ResolutionKind = PointerResolutionKind.Direct,
            PatchAddress = image.LoadDef.PatchAddress,
            Address = image.LoadDef.Address
        };

        context.ResolvePointerValue(
            data,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                PayloadBlock = XFILE_BLOCK.PHYSICAL,
                CountMember = nameof(GfxImageLoadDef.ResourceSize)
            },
            loadDef);

        loadDef.Data = data.Value ?? [];
        image.LoadDef.Address = data.Address;
        image.LoadDef.Value = loadDef;
    }

    private static void PopulateGfxImageRootFields(GfxImage image)
    {
        var root = image.EbootRootPrefix;

        image.Width = ReadUInt16(root, 0x08);
        image.Height = ReadUInt16(root, 0x0A);
        image.Depth = ReadUInt16(root, 0x0C);
        image.MapType = root[0x18];
        image.Semantic = root[0x19];
        image.Category = root[0x1A];
        image.UseSrgbReads = root[0x1B];
        image.Picmip = [root[0x1C], root[0x1D]];
        image.NoPicmip = root[0x1E];
        image.Track = root[0x1F];
        image.CardMemory = [ReadInt32(root, 0x20), ReadInt32(root, 0x24)];

        if (image.EbootRootSuffix.Length > 0x1C)
            image.DelayLoadPixels = image.EbootRootSuffix[0x1C];
    }

    private static int GetGfxImageResourceSize(GfxImage image)
    {
        var format = image.EbootRootPrefix[0x00] & 0x9F;
        var levelCount = image.EbootRootPrefix[0x01];
        if (levelCount == 0)
            return 0;

        var width = Math.Max(1, (int)image.Width);
        var height = Math.Max(1, (int)image.Height);
        var depth = Math.Max(1, (int)image.Depth);
        var totalSize = 0;

        for (var level = 0; level < levelCount; level++)
        {
            totalSize = checked(totalSize + GetGfxImageLevelSize(format, width, height, depth));

            width = Math.Max(1, width >> 1);
            height = Math.Max(1, height >> 1);
            depth = Math.Max(1, depth >> 1);
        }

        var alignedSize = Align(totalSize, 0x80);
        if (image.EbootRootPrefix[0x03] != 0)
            alignedSize = checked(alignedSize * 6);

        if (alignedSize > 0)
            return alignedSize;

        return image.CardMemory.FirstOrDefault(size => size > 0);
    }

    private static int GetGfxImageLevelSize(
        int format,
        int width,
        int height,
        int depth)
    {
        return format switch
        {
            0x86 => checked(((width + 3) / 4) * ((height + 3) / 4) * depth * 8),
            0x87 or 0x88 => checked(((width + 3) / 4) * ((height + 3) / 4) * depth * 16),
            0x85 => checked(width * height * depth * 4),
            _ => 0
        };
    }

    private static ushort ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, sizeof(ushort)));
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, sizeof(int)));
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }
}
