using FastFile.Loaders.Assets.Image;
using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.LightDef;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.LightDef;

public sealed class LightDefLoader
{
    private readonly GfxImageLoader _imageLoader = new();

    public LightDefAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level LightDef pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            LightDefAsset lightDef = ReadLightDef(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return lightDef;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private LightDefAsset ReadLightDef(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, LightDefAsset.SerializedSize, out XBlockAddress rootAddress);
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"LightDef pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        var rootCursor = new FastFileCursor(rootBytes, rootAddress);
        XPointer<string> namePointer = context.PointerReader.ReadPointer<string>(rootCursor, XPointerResolutionMode.Direct);
        XPointer<GfxImageAsset> imagePointer = context.PointerReader.ReadPointer<GfxImageAsset>(rootCursor, XPointerResolutionMode.AliasCell);
        byte samplerState = rootCursor.ReadByte();
        byte[] pad09To0B = rootCursor.ReadBytes(3);
        uint lmapLookupStart = rootCursor.ReadUInt32();

        if (rootCursor.Offset != LightDefAsset.SerializedSize)
            throw new InvalidDataException($"LightDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{LightDefAsset.SerializedSize:X}.");

        string? name;
        GfxImageAsset? image;
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            image = _imageLoader.LoadFromPointer(cursor, imagePointer.Untyped, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  LightDef root source=0x{sourceOffset:X} name={name ?? "<null>"} image=0x{imagePointer.Raw:X8} " +
            $"samplerState=0x{samplerState:X2} lmapLookupStart=0x{lmapLookupStart:X8} " +
            $"imageName={image?.Name ?? "<null>"} blocks={context.Blocks.DescribePositions()}");

        return new LightDefAsset
        {
            Offset = sourceOffset,
            NamePointer = namePointer,
            Name = name,
            ImagePointer = imagePointer,
            Image = image,
            SamplerState = samplerState,
            Pad09To0B = pad09To0B,
            LmapLookupStart = lmapLookupStart
        };
    }
}
