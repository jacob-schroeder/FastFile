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

    private static readonly XPointerFieldAttribute MaterialUshortArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        PayloadBlock = XFILE_BLOCK.RUNTIME,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(Material.TechniqueSlotCount)
    };

    private static readonly XPointerFieldAttribute MaterialTechniqueSetWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute MaterialTextureTableAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 4,
        CountMember = nameof(Material.TextureCount)
    };

    private static readonly XPointerFieldAttribute MaterialConstantTableAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 16,
        CountMember = nameof(Material.ConstantCount)
    };

    private static readonly XPointerFieldAttribute MaterialStateBitTableAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        Alignment = 4,
        CountMember = nameof(Material.StateBitsCount)
    };

    private static readonly XPointerFieldAttribute MaterialUnknownXStringArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(Material.UnknownXStringCount)
    };

    private static readonly XPointerFieldAttribute GfxStateBitsLoadBitsAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(GfxStateBits.LoadBitsCount)
    };

    private static readonly XPointerFieldAttribute GfxImageWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    private static readonly XPointerFieldAttribute WaterWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.LARGE,
        UseCurrentStream = true,
        Alignment = 4
    };

    private static readonly XPointerFieldAttribute WaterSpectrumArrayAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(Water.ElementCount)
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case Material material:
                Load_Material(material, context);
                return true;

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

            case GfxStateBits stateBits:
                Load_GfxStateBits(stateBits, context);
                return true;

            case Water water:
                Load_Water(water, context);
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

    // PS3 0x10d798 / Xbox Load_Material
    private static void Load_Material(
        Material material,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            Load_MaterialInfo(material.Info, context);

            Load_MaterialUshortArray(material, context);
            Load_MaterialTechniqueSetPtr(material, context);
            Load_MaterialTextureDefArray(material, context);
            Load_MaterialConstantDefArray(material, context);
            Load_GfxStateBitsArray(material, context);
            Load_MaterialXStringArray(material, context);
        });
    }

    // PS3 0x1099c8
    private static void Load_MaterialInfo(
        MaterialInfo info,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(info, nameof(MaterialInfo.NamePtr));
    }

    // PS3 Material +0x90 runtime child
    private static void Load_MaterialUshortArray(
        Material material,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.RUNTIME, () =>
        {
            ResolveNonNullCurrentStreamPointer(
                material.UshortArray,
                context,
                MaterialUshortArrayAttribute,
                material);
        });
    }

    // PS3 0x107ef8 / Xbox Load_MaterialTechniqueSetPtr
    private static void Load_MaterialTechniqueSetPtr(
        Material material,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(
                material.TechniqueSet,
                MaterialTechniqueSetWrapperAttribute,
                material);
        });
    }

    // PS3 0x1091d0 / Xbox Load_MaterialTextureDefArray
    private static void Load_MaterialTextureDefArray(
        Material material,
        IXAssetReaderContext context)
    {
        context.ResolvePointerValue(
            material.TextureTable,
            MaterialTextureTableAttribute,
            material);
    }

    // PS3 0xe88d0 / Xbox Load_MaterialConstantDefArray
    private static void Load_MaterialConstantDefArray(
        Material material,
        IXAssetReaderContext context)
    {
        context.ResolvePointerValue(
            material.ConstantTable,
            MaterialConstantTableAttribute,
            material);
    }

    // PS3 0xfaf70 / Xbox Load_GfxStateBitsArray
    private static void Load_GfxStateBitsArray(
        Material material,
        IXAssetReaderContext context)
    {
        context.ResolvePointerValue(
            material.StateBitTable,
            MaterialStateBitTableAttribute,
            material);
    }

    // PS3 0x10b518 via 0xf3d20
    private static void Load_MaterialXStringArray(
        Material material,
        IXAssetReaderContext context)
    {
        ResolveNonNullCurrentStreamPointer(
            material.UnknownXStringArray,
            context,
            MaterialUnknownXStringArrayAttribute,
            material);
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

    // PS3 0xfadc0
    private static void Load_GfxStateBits(
        GfxStateBits stateBits,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(
                stateBits.LoadBits,
                GfxStateBitsLoadBitsAttribute,
                stateBits);
        });
    }

    // PS3 0x108f50
    private static void Load_Water(
        Water water,
        IXAssetReaderContext context)
    {
        ResolveNonNullCurrentStreamPointer(
            water.H0X,
            context,
            WaterSpectrumArrayAttribute,
            water);

        ResolveNonNullCurrentStreamPointer(
            water.H0Y,
            context,
            WaterSpectrumArrayAttribute,
            water);

        ResolveNonNullCurrentStreamPointer(
            water.WTerm,
            context,
            WaterSpectrumArrayAttribute,
            water);

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(
                water.Image,
                GfxImageWrapperAttribute,
                water);
        });
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
        if (texture.Info is null)
            return;

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            Load_WaterPtr(texture.Info, context);
            return;
        }

        Load_GfxImagePtr(texture.Info, context);
    }

    // PS3 0x109118 -> 0x109060 / Xbox Load_MaterialTextureDef / Info
    private static void Load_GfxImagePtr(
        MaterialTextureDefInfo info,
        IXAssetReaderContext context)
    {
        if (info.DataPtr.IsNull)
            return;

        info.Image = context.ReinterpretPointer<GfxImage>(info.DataPtr, PointerResolutionKind.Direct);
        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(
                info.Image,
                GfxImageWrapperAttribute,
                info);
        });
    }

    // PS3 0x109118 -> 0x109060 -> 0x108f50
    private static void Load_WaterPtr(
        MaterialTextureDefInfo info,
        IXAssetReaderContext context)
    {
        if (info.DataPtr.IsNull)
            return;

        info.Water = context.ReinterpretPointer<Water>(info.DataPtr, PointerResolutionKind.Direct);
        context.ResolvePointerValue(
            info.Water,
            WaterWrapperAttribute,
            info);
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
            LevelCount = image.LevelCount,
            Flags = image.TextureFlags,
            Format = Ps3GfxImagePayloadSize.NormalizeFormatByte(image.FormatByte),
            ResourceSize = Ps3GfxImagePayloadSize.ComputeByteCount(image)
        };

        var payload = MaterializeGfxImagePayload(image.LoadDef, image, loadDef, context);
        loadDef.Data = payload.Value ?? [];

        image.LoadDef = new XPointer<GfxImageLoadDef>
        {
            Raw = image.LoadDef.Raw,
            Kind = PointerKind.Inline,
            ResolutionKind = image.LoadDef.ResolutionKind,
            PatchAddress = image.LoadDef.PatchAddress,
            Address = payload.Address,
            Value = loadDef
        };
    }

    private static void PopulateGfxImageRootFields(GfxImage image)
    {
        var root = image.EbootRootPrefix;

        image.FormatByte = root[0x00];
        image.LevelCount = root[0x01];
        image.Unknown02 = root[0x02];
        image.MultiFaceControl = root[0x03];
        image.TextureFlags = ReadInt32(root, 0x04);
        image.Width = ReadUInt16(root, 0x08);
        image.Height = ReadUInt16(root, 0x0A);
        image.Depth = ReadUInt16(root, 0x0C);
        image.TexturePlatformBytes0E = root.AsSpan(0x0E, 0x0A).ToArray();
        image.MapType = (GfxImageMapType)root[0x18];
        image.TextureSemantic = (MaterialTextureSemantic)root[0x19];
        image.Category = (GfxImageCategory)root[0x1A];
        image.Unknown1B = root[0x1B];
        image.PicmipPlatformBytes = [root[0x1C], root[0x1D]];
        image.NoPicmip = root[0x1E];
        image.Unknown1F = root[0x1F];
        image.CardMemoryPlatformWords = [ReadInt32(root, 0x20), ReadInt32(root, 0x24)];
        image.PlatformTailBytes2C = image.EbootRootSuffix.AsSpan(0x00, 0x1C).ToArray();
        image.Unknown48 = image.EbootRootSuffix[0x1C];
        image.NamePadding = image.EbootRootSuffix.AsSpan(0x1D, 0x03).ToArray();
    }

    private static ushort ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, sizeof(ushort)));
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset, sizeof(int)));
    }

    private static XPointer<byte[]> MaterializeGfxImagePayload(
        XPointer<GfxImageLoadDef> payloadCell,
        GfxImage image,
        GfxImageLoadDef loadDef,
        IXAssetReaderContext context)
    {
        var forcedInline = new XPointer<byte[]>
        {
            // PS3 0xfd7d0 treats any non-null value at GfxImage+0x28 as
            // "payload follows here"; it does not use direct packed-offset
            // semantics in the Material -> GfxImage path.
            Raw = -1,
            Kind = PointerKind.Inline,
            ResolutionKind = PointerResolutionKind.Direct,
            PatchAddress = payloadCell.PatchAddress
        };

        try
        {
            context.ResolvePointerValue(
                forcedInline,
                new XPointerFieldAttribute
                {
                    ResolutionKind = PointerResolutionKind.Direct,
                    Target = XPointerTarget.ByteArray,
                    PayloadBlock = GetGfxImagePayloadBlock(image),
                    Alignment = GfxImage.EBOOT_PAYLOAD_ALIGNMENT,
                    CountMember = nameof(GfxImageLoadDef.ResourceSize)
                },
                loadDef);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            var formatKey = Ps3GfxImagePayloadSize.BuildFormatKey(image.FormatByte, image.TextureFlags);
            throw new InvalidDataException(
                $"Failed to materialize GfxImage payload for '{image.Name}' " +
                $"(formatByte=0x{image.FormatByte:X2}, formatKey=0x{formatKey:X8}, flags=0x{image.TextureFlags:X8}, " +
                $"width={image.Width}, height={image.Height}, depth={image.Depth}, levels={image.LevelCount}, " +
                $"mapType={image.MapType}, textureSemantic={image.TextureSemantic}, category={image.Category}, multiFace=0x{image.MultiFaceControl:X2}, " +
                $"resourceSize=0x{loadDef.ResourceSize:X}, cardMemoryWords=[0x{image.CardMemoryPlatformWords[0]:X},0x{image.CardMemoryPlatformWords[1]:X}]).",
                ex);
        }

        return forcedInline;
    }

    private static XFILE_BLOCK GetGfxImagePayloadBlock(GfxImage image)
    {
        return image.TextureSemantic == MaterialTextureSemantic.TS_WATER_MAP
            ? XFILE_BLOCK.RUNTIME
            : XFILE_BLOCK.PHYSICAL;
    }

    private static void ResolveNonNullCurrentStreamPointer<T>(
        XPointer<T> pointer,
        IXAssetReaderContext context,
        XPointerFieldAttribute attribute,
        object owner)
    {
        if (pointer.IsNull)
            return;

        var forcedInline = new XPointer<T>
        {
            Raw = -1,
            Kind = PointerKind.Inline,
            ResolutionKind = pointer.ResolutionKind,
            PatchAddress = pointer.PatchAddress
        };

        context.ResolvePointerValue(forcedInline, attribute, owner);
        pointer.Address = forcedInline.Address;
        pointer.Value = forcedInline.Value;
    }
}
