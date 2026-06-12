using System.Buffers.Binary;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class MaterialAssetReader : XAssetReadHandler
{
    public override bool TryResolvePointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case MaterialTechnique technique:
                ResolveMaterialTechniquePointers(technique, context);
                return true;

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

    private static void ResolveMaterialTechniquePointers(
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
        context.MaterializeCStringPointer(technique.NamePtr);
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
