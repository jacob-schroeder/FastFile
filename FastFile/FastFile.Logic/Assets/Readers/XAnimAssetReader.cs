using FastFile.Logic.Zone;
using FastFile.Models.Assets.XAnim;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class XAnimAssetReader : XAssetReadHandler
{
    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        switch (value)
        {
            case XAnimParts parts:
                Load_XAnimParts(parts, context);
                return true;

            case XAnimDeltaPart:
            case XAnimPartTrans:
            case XAnimDeltaPartQuat2:
            case XAnimDeltaPartQuat:
                // These need the containing XAnimParts for numFrames-driven
                // union payload sizes, so Load_XAnimParts invokes them after
                // the fixed root has been materialized.
                return true;

            default:
                return false;
        }
    }

    // PS3 0x115fc0 / Xbox Load_XAnimParts.
    private static void Load_XAnimParts(
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(parts, nameof(XAnimParts.NamePtr));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.Names));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.Notify));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DeltaPart));
            if (parts.DeltaPart.Value is { } deltaPart)
                Load_XAnimDeltaPart(deltaPart, parts, context);

            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataByte));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataShort));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataInt));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataShort));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataByte));
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataInt));
            Load_XAnimIndices(parts, context);
        });
    }

    // PS3 0xf5b18: the root +0x4c XAnimIndices payload is byte-sized when
    // numFrames <= 255, otherwise ushort-sized.
    private static void Load_XAnimIndices(
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        if (parts.Indices.Kind == PointerKind.Null)
            return;

        if (parts.NumFrames <= byte.MaxValue)
        {
            parts.ByteIndices = XPointerCodec.ReinterpretPointer<byte[]>(
                parts.Indices,
                PointerResolutionKind.Direct);

            context.ResolvePointerValue(
                parts.ByteIndices,
                CreateRawByteArrayAttribute(1, nameof(XAnimParts.IndexCount)),
                parts);

            return;
        }

        parts.UShortIndices = XPointerCodec.ReinterpretPointer<ushort[]>(
            parts.Indices,
            PointerResolutionKind.Direct);

        context.ResolvePointerValue(
            parts.UShortIndices,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                UseCurrentStream = true,
                Alignment = 2,
                CountMember = nameof(XAnimParts.IndexCount)
            },
            parts);
    }

    // PS3 0xf4420 / Xbox Load_XAnimDeltaPart.
    private static void Load_XAnimDeltaPart(
        XAnimDeltaPart deltaPart,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Trans));
        if (deltaPart.Trans.Value is { } trans)
            Load_XAnimPartTrans(trans, parts, context);

        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Quat2));
        if (deltaPart.Quat2.Value is { } quat2)
            Load_XAnimDeltaPartQuat2(quat2, parts, context);

        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Quat));
        if (deltaPart.Quat.Value is { } quat)
            Load_XAnimDeltaPartQuat(quat, parts, context);
    }

    // PS3 0xf3618 -> 0xf35d8 / Xbox Load_XAnimPartTrans.
    private static void Load_XAnimPartTrans(
        XAnimPartTrans trans,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        if (trans.Size == 0)
        {
            trans.Frame0 = context.ReadCurrentStreamObject<XAnimPartTransFrame0>();
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimPartTransFrames>();
        int frameCount = trans.Size + 1;

        frames.DynamicFrameByteCount = frameCount * GetDynamicVec3ByteSize(parts);
        frames.DynamicFrames = context.ReadCurrentStreamBytes(frames.DynamicFrameByteCount);
        frames.IndexByteCount = frameCount * (trans.SmallTrans != 0 ? 3 : 6);

        context.ResolvePointerValue(
            frames.Indices,
            CreateRawByteArrayAttribute(
                trans.SmallTrans != 0 ? 1 : 4,
                nameof(XAnimPartTransFrames.IndexByteCount)),
            frames);

        trans.Frames = frames;
    }

    // PS3 0xf3f50 -> 0xf3f10 / Xbox Load_XAnimDeltaPartQuat2.
    private static void Load_XAnimDeltaPartQuat2(
        XAnimDeltaPartQuat2 quat,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        if (quat.Size == 0)
        {
            quat.Frame0 = context.ReadCurrentStreamObject<XQuat2>();
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimDeltaPartQuatDataFrames2>();
        frames.FrameCount = quat.Size + 1;
        frames.DynamicIndexByteCount = GetDynamicIndexByteCount(parts, frames.FrameCount);
        frames.DynamicIndices = context.ReadCurrentStreamBytes(frames.DynamicIndexByteCount);
        context.ResolvePointerProperty(frames, nameof(XAnimDeltaPartQuatDataFrames2.Frames));
        quat.Frames = frames;
    }

    // PS3 0xf43d0 -> 0xf4390 / Xbox Load_XAnimDeltaPartQuat.
    private static void Load_XAnimDeltaPartQuat(
        XAnimDeltaPartQuat quat,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        if (quat.Size == 0)
        {
            quat.Frame0 = context.ReadCurrentStreamObject<XQuat>();
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimDeltaPartQuatDataFrames>();
        frames.FrameCount = quat.Size + 1;
        frames.DynamicIndexByteCount = GetDynamicIndexByteCount(parts, frames.FrameCount);
        frames.DynamicIndices = context.ReadCurrentStreamBytes(frames.DynamicIndexByteCount);
        context.ResolvePointerProperty(frames, nameof(XAnimDeltaPartQuatDataFrames.Frames));
        quat.Frames = frames;
    }

    private static int GetDynamicVec3ByteSize(XAnimParts parts)
    {
        return parts.NumFrames <= byte.MaxValue ? 3 : 6;
    }

    private static int GetDynamicIndexByteCount(
        XAnimParts parts,
        int frameCount)
    {
        return frameCount * (parts.NumFrames <= byte.MaxValue ? 1 : 2);
    }

    private static XPointerFieldAttribute CreateRawByteArrayAttribute(
        int alignment,
        string countMember)
    {
        return new XPointerFieldAttribute
        {
            ResolutionKind = PointerResolutionKind.Direct,
            Target = XPointerTarget.ByteArray,
            UseCurrentStream = true,
            Alignment = alignment,
            CountMember = countMember
        };
    }
}
