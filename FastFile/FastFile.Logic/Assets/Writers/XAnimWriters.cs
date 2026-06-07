using System.Buffers.Binary;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private static void WriteXAnimParts(XFileWriterContext context, XAnimParts asset)
    {
        var root = BuildXAnimRoot(asset);

        context.WritePointerRaw(asset.NamePtr, PointerResolutionKind.Direct, "XAnimParts.Name");
        context.WriteBytes(root.AsSpan(0x04, 0x2c));
        context.WritePointerRaw(asset.NamesPtr, PointerResolutionKind.Direct, "XAnimParts.Names");
        context.WritePointerRaw(asset.DataBytePtr, PointerResolutionKind.Direct, "XAnimParts.DataByte");
        context.WritePointerRaw(asset.DataShortPtr, PointerResolutionKind.Direct, "XAnimParts.DataShort");
        context.WritePointerRaw(asset.DataIntPtr, PointerResolutionKind.Direct, "XAnimParts.DataInt");
        context.WritePointerRaw(asset.RandomDataShortPtr, PointerResolutionKind.Direct, "XAnimParts.RandomDataShort");
        context.WritePointerRaw(asset.RandomDataBytePtr, PointerResolutionKind.Direct, "XAnimParts.RandomDataByte");
        context.WritePointerRaw(asset.RandomDataIntPtr, PointerResolutionKind.Direct, "XAnimParts.RandomDataInt");
        context.WritePointerRaw(asset.IndicesPtr, PointerResolutionKind.Direct, "XAnimParts.Indices");
        context.WritePointerRaw(asset.NotifyPtr, PointerResolutionKind.Direct, "XAnimParts.Notify");
        context.WritePointerRaw(asset.DeltaPartPtr, PointerResolutionKind.Direct, "XAnimParts.DeltaPart");

        WriteQueuedLargeString(context, asset.NamePtr);
        WriteQueuedXAnimRaw(context, asset.NamesPtr, checked(asset.BoneNameCount * 2), XFileStreamAlignment.Two);
        WriteQueuedXAnimRaw(context, asset.NotifyPtr, checked(asset.NotifyCount * 8), XFileStreamAlignment.Four);
        WriteQueuedXAnimDeltaPart(context, asset);
        WriteQueuedXAnimRaw(context, asset.DataBytePtr, asset.DataByteCount, XFileStreamAlignment.Byte);
        WriteQueuedXAnimRaw(context, asset.DataShortPtr, checked(asset.DataShortCount * 2), XFileStreamAlignment.Two);
        WriteQueuedXAnimRaw(context, asset.DataIntPtr, checked(asset.DataIntCount * 4), XFileStreamAlignment.Four);
        WriteQueuedXAnimRaw(context, asset.RandomDataShortPtr, checked(Positive(asset.RandomDataShortCount) * 2), XFileStreamAlignment.Two);
        WriteQueuedXAnimRaw(context, asset.RandomDataBytePtr, asset.RandomDataByteCount, XFileStreamAlignment.Byte);
        WriteQueuedXAnimRaw(context, asset.RandomDataIntPtr, checked(asset.RandomDataIntCount * 4), XFileStreamAlignment.Four);
        WriteQueuedXAnimRaw(
            context,
            asset.IndicesPtr,
            checked(Positive(asset.IndexCount) * XAnimIndexElementSize(asset.FrameCount)),
            asset.FrameCount <= 255 ? XFileStreamAlignment.Byte : XFileStreamAlignment.Two);
    }

    private static void WriteQueuedXAnimRaw(
        XFileWriterContext context,
        ZonePointer<byte[]>? pointer,
        int byteCount,
        XFileStreamAlignment alignment)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedXAnimRaw(context, pointer, byteCount, alignment)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        context.Align(alignment);
        context.RegisterMaterializedPointerValue(pointer, Math.Max(0, byteCount));
        WriteFixedBytes(context, pointer.Result, Math.Max(0, byteCount));
    }

    private static void WriteQueuedXAnimDeltaPart(XFileWriterContext context, XAnimParts asset)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedXAnimDeltaPart(context, asset)))
            return;

        var pointer = asset.DeltaPartPtr;
        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        var delta = pointer.Result;
        context.Align(XFileStreamAlignment.Four);
        context.RegisterMaterializedPointerValue(pointer, 0x0c);
        context.WritePointerRaw(delta.TransPtr, PointerResolutionKind.Direct, "XAnimDeltaPart.Trans");
        context.WritePointerRaw(delta.QuatPtr, PointerResolutionKind.Direct, "XAnimDeltaPart.Quat");
        context.WritePointerRaw(delta.Quat2Ptr, PointerResolutionKind.Direct, "XAnimDeltaPart.Quat2");

        WriteQueuedXAnimDeltaTrans(context, delta.TransPtr, asset);
        WriteQueuedXAnimDeltaQuat(context, delta.QuatPtr, asset, frame0Size: 4, frameDataStride: 4);
        WriteQueuedXAnimDeltaQuat(context, delta.Quat2Ptr, asset, frame0Size: 8, frameDataStride: 8);
    }

    private static void WriteQueuedXAnimDeltaTrans(
        XFileWriterContext context,
        ZonePointer<XAnimDeltaTrans>? pointer,
        XAnimParts asset)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedXAnimDeltaTrans(context, pointer, asset)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        var trans = pointer.Result;
        context.Align(XFileStreamAlignment.Four);
        context.RegisterMaterializedPointerValue(pointer);
        WriteFixedBytes(context, trans.Header, 4);

        if (trans.Size == 0)
        {
            WriteFixedBytes(context, trans.Frame0, 12);
            return;
        }

        var frameCount = checked(trans.Size + 1);
        WriteFixedBytes(context, trans.FrameTable, 0x1c);
        WriteFixedBytes(context, trans.FrameIndices, checked(frameCount * XAnimIndexElementSize(asset.FrameCount)));
        WriteQueuedXAnimRaw(
            context,
            trans.FrameDataPtr,
            checked(frameCount * (trans.IsSmall ? 3 : 6)),
            trans.IsSmall ? XFileStreamAlignment.Byte : XFileStreamAlignment.Four);
    }

    private static void WriteQueuedXAnimDeltaQuat(
        XFileWriterContext context,
        ZonePointer<XAnimDeltaQuat>? pointer,
        XAnimParts asset,
        int frame0Size,
        int frameDataStride)
    {
        if (context.TryDeferInlineWrite(() => WriteQueuedXAnimDeltaQuat(context, pointer, asset, frame0Size, frameDataStride)))
            return;

        if (pointer is not { IsInlineData: true, Result: not null })
            return;

        var quat = pointer.Result;
        context.Align(XFileStreamAlignment.Four);
        context.RegisterMaterializedPointerValue(pointer);
        WriteFixedBytes(context, quat.Header, 4);

        if (quat.Size == 0)
        {
            WriteFixedBytes(context, quat.Frame0, frame0Size);
            return;
        }

        var frameCount = checked(quat.Size + 1);
        WriteFixedBytes(context, quat.FrameTable, 4);
        WriteFixedBytes(context, quat.FrameIndices, checked(frameCount * XAnimIndexElementSize(asset.FrameCount)));
        WriteQueuedXAnimRaw(
            context,
            quat.FrameDataPtr,
            checked(frameCount * frameDataStride),
            XFileStreamAlignment.Four);
    }

    private static byte[] BuildXAnimRoot(XAnimParts asset)
    {
        var root = new byte[XAnimParts.RootSize];
        if (asset.RawRoot.Length > 0)
            asset.RawRoot.AsSpan(0, Math.Min(asset.RawRoot.Length, root.Length)).CopyTo(root);

        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x04, 2), asset.DataByteCount);
        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x06, 2), asset.DataShortCount);
        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x08, 2), asset.DataIntCount);
        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x0a, 2), asset.RandomDataByteCount);
        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x0c, 2), asset.RandomDataIntCount);
        BinaryPrimitives.WriteUInt16BigEndian(root.AsSpan(0x0e, 2), asset.FrameCount);
        asset.RootBytes10To1F.AsSpan(0, Math.Min(asset.RootBytes10To1F.Length, 0x10)).CopyTo(root.AsSpan(0x10, 0x10));
        BinaryPrimitives.WriteInt32BigEndian(root.AsSpan(0x20, 4), asset.RandomDataShortCount);
        BinaryPrimitives.WriteInt32BigEndian(root.AsSpan(0x24, 4), asset.IndexCount);
        BinaryPrimitives.WriteSingleBigEndian(root.AsSpan(0x28, 4), asset.Framerate);
        BinaryPrimitives.WriteSingleBigEndian(root.AsSpan(0x2c, 4), asset.Frequency);

        return root;
    }

    private static int XAnimIndexElementSize(ushort frameCount)
    {
        return frameCount <= 255 ? 1 : 2;
    }

    private static int Positive(int count)
    {
        return Math.Max(0, count);
    }
}
