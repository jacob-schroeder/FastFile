using System.Buffers.Binary;
using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Material;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class ImageReader
{
    private const int WidthOffset = 0x08;
    private const int HeightOffset = 0x0A;
    private const int DepthOffset = 0x0C;
    private const int UseSrgbReadsOffset = 0x18;
    private const int MapTypeOffset = 0x19;
    private const int SemanticOffset = 0x1A;
    private const int CategoryOffset = 0x1B;
    private const int ResourceSizeOffset = 0x1C;
    private const int CardMemoryOffset = 0x20;
    private static int _traceImageCount;

    public static GfxImage Read(ref XFileReadContext context)
    {
        return Read(ref context, resolveChildrenNow: false);
    }

    private static GfxImage Read(ref XFileReadContext context, bool resolveChildrenNow)
    {
        var asset = new GfxImage
        {
            Offset = context.Position,
            EbootRootPrefix = context.ReadBytes(GfxImage.EBOOT_LOAD_DEF_POINTER_OFFSET),
        };

        ApplyRootPrefix(asset);

        asset.LoadDef = context.ReadDirectPointer<GfxImageLoadDef>("GfxImage.LoadDef");
        asset.EbootRootSuffix = context.ReadBytes(
            GfxImage.EBOOT_NAME_POINTER_OFFSET
            - GfxImage.EBOOT_LOAD_DEF_POINTER_OFFSET
            - XFileWriteRules.PointerSize);
        asset.NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false);

        TraceImageRoot(asset);
        ResolveImageChildren(ref context, asset, resolveChildrenNow);

        return asset;
    }

    public static ZonePointer<GfxImage> ReadImagePointer(ref XFileReadContext context)
    {
        var pointer = ReadImagePointerField(ref context);
        ResolveImagePointer(ref context, pointer);
        return pointer;
    }

    public static ZonePointer<GfxImage> ReadImagePointerField(ref XFileReadContext context)
    {
        return context.ReadAliasPointer<GfxImage>("GfxImageAssetRef");
    }

    public static void ResolveImagePointer(
        ref XFileReadContext context,
        ZonePointer<GfxImage> pointer)
    {
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            ReadImagePointerValue);
    }

    public static void ResolveImagePointerNow(
        ref XFileReadContext context,
        ZonePointer<GfxImage> pointer)
    {
        context.ResolvePointerNowInBlock(pointer, XFILE_BLOCK.TEMP, ReadImagePointerValueNow);
    }

    private static void ReadImagePointerValue(
        ref XFileReadContext context,
        ZonePointer<GfxImage> pointer)
    {
        var value = context.ReadPointerValue(pointer, Read);
        pointer.SetResult(value);
    }

    private static void ReadImagePointerValueNow(
        ref XFileReadContext context,
        ZonePointer<GfxImage> pointer)
    {
        var value = context.ReadPointerValue(
            pointer,
            (ref XFileReadContext valueContext) => Read(ref valueContext, resolveChildrenNow: true));
        pointer.SetResult(value);
    }

    private static void ResolveImageChildren(
        ref XFileReadContext context,
        GfxImage asset,
        bool resolveNow)
    {
        if (resolveNow)
        {
            context.ResolvePointerNowInBlock(
                asset.NamePtr,
                XFILE_BLOCK.LARGE,
                GenericReader.ReadStringPointerValue);
            ResolveImageLoadDefNow(ref context, asset);
            return;
        }

        context.ResolvePointerInBlock(
            asset.NamePtr,
            XFILE_BLOCK.LARGE,
            GenericReader.ReadStringPointerValue);
        ResolveImageLoadDef(ref context, asset);
    }

    private static void ResolveImageLoadDef(ref XFileReadContext context, GfxImage asset)
    {
        var payloadBlock = GetPixelDataBlock(asset);
        context.ResolvePointerInBlock(
            asset.LoadDef,
            payloadBlock,
            (ref XFileReadContext pointerContext, ZonePointer<GfxImageLoadDef> pointer) =>
            {
                pointerContext.AlignStreamOnly(XFileStreamAlignment.OneTwentyEight);
                var value = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext dataContext) => ReadImageLoadDefBytes(ref dataContext, asset));
                pointer.SetResult(value);
            });
    }

    private static void ResolveImageLoadDefNow(ref XFileReadContext context, GfxImage asset)
    {
        var payloadBlock = GetPixelDataBlock(asset);
        context.ResolvePointerNowInBlock(
            asset.LoadDef,
            payloadBlock,
            (ref XFileReadContext pointerContext, ZonePointer<GfxImageLoadDef> pointer) =>
            {
                pointerContext.AlignStreamOnly(XFileStreamAlignment.OneTwentyEight);
                var value = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext dataContext) => ReadImageLoadDefBytes(ref dataContext, asset));
                pointer.SetResult(value);
            });
    }

    private static GfxImageLoadDef ReadImageLoadDefBytes(ref XFileReadContext context, GfxImage asset)
    {
        var loadDef = CreateLoadDefFromRoot(asset);
        if (loadDef.ResourceSize > 0)
        {
            if (loadDef.ResourceSize > context.Span.Length - context.Position)
            {
                var prefix = Convert.ToHexString(asset.EbootRootPrefix);
                throw new InvalidDataException(
                    $"Image pixel payload size 0x{loadDef.ResourceSize:X8} is outside the remaining zone stream at image offset 0x{asset.Offset:X8}; "
                    + $"nameRaw=0x{asset.NamePtr.Raw:X8}, {asset.Width}x{asset.Height}x{asset.Depth}, map={asset.MapType}, semantic={asset.Semantic}, category={asset.Category}, "
                    + $"delayLoad={asset.DelayLoadPixels}, prefix={prefix}.");
            }

            loadDef.Data = context.ReadBytes(loadDef.ResourceSize);
        }

        return loadDef;
    }

    private static void TraceImageRoot(GfxImage asset)
    {
        if (Environment.GetEnvironmentVariable("FASTFILE_TRACE_IMAGES") != "1")
            return;

        var count = Interlocked.Increment(ref _traceImageCount);
        if (count > 512)
            return;

        var resourceSize = ReadInt32(asset.EbootRootPrefix, ResourceSizeOffset);
        Console.Error.WriteLine(
            $"image[{count:D3}] off=0x{asset.Offset:X8} rawSize=0x{resourceSize:X8} "
            + $"dims={asset.Width}x{asset.Height}x{asset.Depth} map={asset.MapType} sem={asset.Semantic} cat={asset.Category} "
            + $"delay={asset.DelayLoadPixels} loadRaw=0x{asset.LoadDef.Raw:X8} nameRaw=0x{asset.NamePtr.Raw:X8} prefix={Convert.ToHexString(asset.EbootRootPrefix)}");
    }

    private static GfxImageLoadDef CreateLoadDefFromRoot(GfxImage asset)
    {
        var prefix = asset.EbootRootPrefix;
        var loadDef = new GfxImageLoadDef
        {
            LevelCount = GetByte(prefix, 0),
            Pad = GetBytes(prefix, 1, 3),
            Flags = ReadInt32(prefix, 4),
            // PS3 image roots pack the GCM texture format in byte 0; byte 7 is part of the flags word.
            Format = GetByte(prefix, 0),
            ResourceSize = ReadInt32(prefix, ResourceSizeOffset),
        };

        return loadDef;
    }

    private static void ApplyRootPrefix(GfxImage asset)
    {
        var prefix = asset.EbootRootPrefix;

        asset.Width = ReadUInt16(prefix, WidthOffset);
        asset.Height = ReadUInt16(prefix, HeightOffset);
        asset.Depth = ReadUInt16(prefix, DepthOffset);
        asset.UseSrgbReads = GetByte(prefix, UseSrgbReadsOffset);
        asset.MapType = GetByte(prefix, MapTypeOffset);
        asset.Semantic = GetByte(prefix, SemanticOffset);
        asset.Category = GetByte(prefix, CategoryOffset);
        asset.Picmip = GetBytes(prefix, 1, 2);
        asset.NoPicmip = GetByte(prefix, 3);
        asset.Track = GetByte(prefix, UseSrgbReadsOffset);
        asset.CardMemory =
        [
            ReadInt32(prefix, CardMemoryOffset),
            ReadInt32(prefix, CardMemoryOffset + 4)
        ];
        asset.DelayLoadPixels = GetByte(prefix, 0x26);
        asset.Pad = GetBytes(prefix, 0x15, 3);
    }

    private static XFILE_BLOCK GetPixelDataBlock(GfxImage asset)
    {
        return asset.MapType == 11
            ? XFILE_BLOCK.RUNTIME
            : XFILE_BLOCK.PHYSICAL;
    }

    private static byte GetByte(byte[] value, int offset)
    {
        return offset >= 0 && offset < value.Length ? value[offset] : (byte)0;
    }

    private static byte[] GetBytes(byte[] value, int offset, int count)
    {
        var bytes = new byte[count];
        if (offset < 0 || offset >= value.Length || count <= 0)
            return bytes;

        Array.Copy(value, offset, bytes, 0, Math.Min(count, value.Length - offset));
        return bytes;
    }

    private static int ReadInt32(byte[] value, int offset)
    {
        return offset >= 0 && offset + 4 <= value.Length
            ? BinaryPrimitives.ReadInt32BigEndian(value.AsSpan(offset, 4))
            : 0;
    }

    private static ushort ReadUInt16(byte[] value, int offset)
    {
        return offset >= 0 && offset + 2 <= value.Length
            ? BinaryPrimitives.ReadUInt16BigEndian(value.AsSpan(offset, 2))
            : (ushort)0;
    }
}
