using System.Buffers.Binary;
using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class XAnimReader
{
    public static XAnimParts Read(ref XFileReadContext context)
    {
        var rootOffset = context.Position;
        var rawRoot = context.Span.Slice(rootOffset, XAnimParts.RootSize).ToArray();

        var asset = new XAnimParts
        {
            Offset = rootOffset,
            RawRoot = rawRoot,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            DataByteCount = context.ReadUInt16(),
            DataShortCount = context.ReadUInt16(),
            DataIntCount = context.ReadUInt16(),
            RandomDataByteCount = context.ReadUInt16(),
            RandomDataIntCount = context.ReadUInt16(),
            FrameCount = context.ReadUInt16(),
            RootBytes10To1F = context.ReadBytes(0x10),
            RandomDataShortCount = context.ReadInt32(),
            IndexCount = context.ReadInt32(),
            Framerate = context.ReadFloat(),
            Frequency = context.ReadFloat(),
            NamesPtr = context.ReadDirectPointer<byte[]>("XAnimParts.Names"),
            DataBytePtr = context.ReadDirectPointer<byte[]>("XAnimParts.DataByte"),
            DataShortPtr = context.ReadDirectPointer<byte[]>("XAnimParts.DataShort"),
            DataIntPtr = context.ReadDirectPointer<byte[]>("XAnimParts.DataInt"),
            RandomDataShortPtr = context.ReadDirectPointer<byte[]>("XAnimParts.RandomDataShort"),
            RandomDataBytePtr = context.ReadDirectPointer<byte[]>("XAnimParts.RandomDataByte"),
            RandomDataIntPtr = context.ReadDirectPointer<byte[]>("XAnimParts.RandomDataInt"),
            IndicesPtr = context.ReadDirectPointer<byte[]>("XAnimParts.Indices"),
            NotifyPtr = context.ReadDirectPointer<byte[]>("XAnimParts.Notify"),
            DeltaPartPtr = context.ReadDirectPointer<XAnimDeltaPart>("XAnimParts.DeltaPart"),
        };

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);
        ResolveRawPointer(ref context, asset.NamesPtr, checked(asset.BoneNameCount * 2), XFileStreamAlignment.Two);
        ResolveRawPointer(ref context, asset.NotifyPtr, checked(asset.NotifyCount * 8), XFileStreamAlignment.Four);
        ResolveDeltaPart(ref context, asset);
        ResolveRawPointer(ref context, asset.DataBytePtr, asset.DataByteCount, XFileStreamAlignment.Byte);
        ResolveRawPointer(ref context, asset.DataShortPtr, checked(asset.DataShortCount * 2), XFileStreamAlignment.Two);
        ResolveRawPointer(ref context, asset.DataIntPtr, checked(asset.DataIntCount * 4), XFileStreamAlignment.Four);
        ResolveRawPointer(ref context, asset.RandomDataShortPtr, checked(Positive(asset.RandomDataShortCount) * 2), XFileStreamAlignment.Two);
        ResolveRawPointer(ref context, asset.RandomDataBytePtr, asset.RandomDataByteCount, XFileStreamAlignment.Byte);
        ResolveRawPointer(ref context, asset.RandomDataIntPtr, checked(asset.RandomDataIntCount * 4), XFileStreamAlignment.Four);
        ResolveRawPointer(
            ref context,
            asset.IndicesPtr,
            checked(Positive(asset.IndexCount) * XAnimIndexElementSize(asset.FrameCount)),
            asset.FrameCount <= 255 ? XFileStreamAlignment.Byte : XFileStreamAlignment.Two);

        return asset;
    }

    private static void ResolveRawPointer(
        ref XFileReadContext context,
        ZonePointer<byte[]>? pointer,
        int byteCount,
        XFileStreamAlignment alignment)
    {
        if (pointer is null)
            return;

        context.ResolvePointerAlignedInBlock(
            pointer,
            XFILE_BLOCK.LARGE,
            alignment,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> p) =>
                p.SetResult(pointerContext.ReadBytes(Math.Max(0, byteCount))));
    }

    private static void ResolveDeltaPart(ref XFileReadContext context, XAnimParts asset)
    {
        if (asset.DeltaPartPtr is null)
            return;

        context.ResolvePointerAlignedInBlock(
            asset.DeltaPartPtr,
            XFILE_BLOCK.LARGE,
            XFileStreamAlignment.Four,
            (ref XFileReadContext pointerContext, ZonePointer<XAnimDeltaPart> pointer) =>
                pointer.SetResult(ReadDeltaPart(ref pointerContext, asset)));
    }

    private static XAnimDeltaPart ReadDeltaPart(ref XFileReadContext context, XAnimParts asset)
    {
        var delta = new XAnimDeltaPart
        {
            Offset = context.Position,
            TransPtr = context.ReadDirectPointer<XAnimDeltaTrans>("XAnimDeltaPart.Trans"),
            QuatPtr = context.ReadDirectPointer<XAnimDeltaQuat>("XAnimDeltaPart.Quat"),
            Quat2Ptr = context.ReadDirectPointer<XAnimDeltaQuat>("XAnimDeltaPart.Quat2"),
        };
        delta.Raw = context.Span.Slice(delta.Offset, 0x0c).ToArray();

        context.ResolvePointerAligned(
            delta.TransPtr,
            XFileStreamAlignment.Four,
            (ref XFileReadContext pointerContext, ZonePointer<XAnimDeltaTrans> pointer) =>
                pointer.SetResult(ReadDeltaTrans(ref pointerContext, asset)));
        context.ResolvePointerAligned(
            delta.QuatPtr,
            XFileStreamAlignment.Four,
            (ref XFileReadContext pointerContext, ZonePointer<XAnimDeltaQuat> pointer) =>
                pointer.SetResult(ReadDeltaQuat(ref pointerContext, asset, frame0Size: 4, frameDataStride: 4)));
        context.ResolvePointerAligned(
            delta.Quat2Ptr,
            XFileStreamAlignment.Four,
            (ref XFileReadContext pointerContext, ZonePointer<XAnimDeltaQuat> pointer) =>
                pointer.SetResult(ReadDeltaQuat(ref pointerContext, asset, frame0Size: 8, frameDataStride: 8)));

        return delta;
    }

    private static XAnimDeltaTrans ReadDeltaTrans(ref XFileReadContext context, XAnimParts asset)
    {
        var trans = new XAnimDeltaTrans
        {
            Offset = context.Position,
            Header = context.ReadBytes(4),
        };
        trans.Size = ReadUInt16(trans.Header, 0);
        trans.IsSmall = trans.Header.Length > 2 && trans.Header[2] != 0;

        if (trans.Size == 0)
        {
            trans.Frame0 = context.ReadBytes(12);
            return trans;
        }

        var frameCount = checked(trans.Size + 1);
        trans.FrameTable = context.ReadBytes(0x1c);
        trans.FrameIndices = context.ReadBytes(checked(frameCount * XAnimIndexElementSize(asset.FrameCount)));
        trans.FrameDataPtr = CreatePayloadPointer(
            ReadInt32(trans.FrameTable, 0x18),
            "XAnimDeltaTrans.FrameData");
        ResolveInlineRawPointer(
            ref context,
            trans.FrameDataPtr,
            checked(frameCount * (trans.IsSmall ? 3 : 6)),
            trans.IsSmall ? XFileStreamAlignment.Byte : XFileStreamAlignment.Four);

        return trans;
    }

    private static XAnimDeltaQuat ReadDeltaQuat(
        ref XFileReadContext context,
        XAnimParts asset,
        int frame0Size,
        int frameDataStride)
    {
        var quat = new XAnimDeltaQuat
        {
            Offset = context.Position,
            Header = context.ReadBytes(4),
        };
        quat.Size = ReadUInt16(quat.Header, 0);

        if (quat.Size == 0)
        {
            quat.Frame0 = context.ReadBytes(frame0Size);
            return quat;
        }

        var frameCount = checked(quat.Size + 1);
        quat.FrameTable = context.ReadBytes(4);
        quat.FrameIndices = context.ReadBytes(checked(frameCount * XAnimIndexElementSize(asset.FrameCount)));
        quat.FrameDataPtr = CreatePayloadPointer(
            ReadInt32(quat.FrameTable, 0),
            frameDataStride == 8 ? "XAnimDeltaQuat2.FrameData" : "XAnimDeltaQuat.FrameData");
        ResolveInlineRawPointer(
            ref context,
            quat.FrameDataPtr,
            checked(frameCount * frameDataStride),
            XFileStreamAlignment.Four);

        return quat;
    }

    private static void ResolveInlineRawPointer(
        ref XFileReadContext context,
        ZonePointer<byte[]>? pointer,
        int byteCount,
        XFileStreamAlignment alignment)
    {
        if (pointer is null)
            return;

        context.ResolvePointerAligned(
            pointer,
            alignment,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> p) =>
                p.SetResult(pointerContext.ReadBytes(Math.Max(0, byteCount))));
    }

    private static ZonePointer<byte[]> CreatePayloadPointer(int raw, string fieldPath)
    {
        var pointer = new ZonePointer<byte[]>(raw);
        pointer.SetResolutionKind(PointerResolutionKind.Direct, fieldPath);
        return pointer;
    }

    private static int XAnimIndexElementSize(ushort frameCount)
    {
        return frameCount <= 255 ? 1 : 2;
    }

    private static int Positive(int count)
    {
        return Math.Max(0, count);
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
    }
}
