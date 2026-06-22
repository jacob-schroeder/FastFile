using FastFile.LogicOLD.Zone;
using FastFile.ModelsOLD.Assets.XAnim;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.LogicOLD.Assets.Readers;

public sealed class XAnimAssetReader : XAssetReadHandler
{
    private static readonly bool TraceXAnimEnabled = Environment.GetEnvironmentVariable("FF_TRACE_XANIM") == "1";

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
        TraceXAnim(
            context,
            $"Load_XAnimParts begin nameRaw=0x{parts.NamePtr.Raw:X8} frames={parts.NumFrames} " +
            $"boneNames={parts.BoneNameCount} notify={parts.NotifyCount} indexCount={parts.IndexCount} " +
            $"deltaRaw=0x{parts.DeltaPart.Raw:X8}");
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            context.ResolvePointerProperty(parts, nameof(XAnimParts.NamePtr));
            TraceXAnim(context, $"name=\"{parts.Name}\"");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.Names));
            TraceXAnim(context, $"names done raw=0x{parts.Names.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.Notify));
            TraceXAnim(context, $"notify done raw=0x{parts.Notify.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DeltaPart));
            TraceXAnim(context, $"deltaPart done raw=0x{parts.DeltaPart.Raw:X8}");
            if (parts.DeltaPart.Value is { } deltaPart)
                Load_XAnimDeltaPart(deltaPart, parts, context);

            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataByte));
            TraceXAnim(context, $"dataByte done raw=0x{parts.DataByte.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataShort));
            TraceXAnim(context, $"dataShort done raw=0x{parts.DataShort.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.DataInt));
            TraceXAnim(context, $"dataInt done raw=0x{parts.DataInt.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataShort));
            TraceXAnim(context, $"randomDataShort done raw=0x{parts.RandomDataShort.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataByte));
            TraceXAnim(context, $"randomDataByte done raw=0x{parts.RandomDataByte.Raw:X8}");
            context.ResolvePointerProperty(parts, nameof(XAnimParts.RandomDataInt));
            TraceXAnim(context, $"randomDataInt done raw=0x{parts.RandomDataInt.Raw:X8}");
            Load_XAnimIndices(parts, context);
            TraceXAnim(context, $"indices done raw=0x{parts.Indices.Raw:X8}");
        });
        TraceXAnim(context, "Load_XAnimParts end");
    }

    // PS3 0xf5b18: the root +0x4c XAnimIndices payload is byte-sized when
    // numFrames <= 255, otherwise ushort-sized.
    private static void Load_XAnimIndices(
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        if (parts.Indices.Kind == PointerKind.Null)
        {
            TraceXAnim(context, "Load_XAnimIndices null");
            return;
        }

        if (parts.NumFrames <= byte.MaxValue)
        {
            parts.ByteIndices = XPointerCodec.ReinterpretPointer<byte[]>(
                parts.Indices,
                PointerResolutionKind.Direct);

            context.ResolvePointerValue(
                parts.ByteIndices,
                CreateRawByteArrayAttribute(1, nameof(XAnimParts.IndexCount)),
                parts);
            TraceXAnim(context, $"Load_XAnimIndices byte count={parts.IndexCount}");

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
        TraceXAnim(context, $"Load_XAnimIndices ushort count={parts.IndexCount}");
    }

    // PS3 0xf4420 / Xbox Load_XAnimDeltaPart.
    private static void Load_XAnimDeltaPart(
        XAnimDeltaPart deltaPart,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        TraceXAnim(
            context,
            $"Load_XAnimDeltaPart begin transRaw=0x{deltaPart.Trans.Raw:X8} quat2Raw=0x{deltaPart.Quat2.Raw:X8} quatRaw=0x{deltaPart.Quat.Raw:X8}");
        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Trans));
        if (deltaPart.Trans.Value is { } trans)
            Load_XAnimPartTrans(trans, parts, context);

        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Quat2));
        if (deltaPart.Quat2.Value is { } quat2)
            Load_XAnimDeltaPartQuat2(quat2, parts, context);

        context.ResolvePointerProperty(deltaPart, nameof(XAnimDeltaPart.Quat));
        if (deltaPart.Quat.Value is { } quat)
            Load_XAnimDeltaPartQuat(quat, parts, context);
        TraceXAnim(context, "Load_XAnimDeltaPart end");
    }

    // PS3 0xf3618 -> 0xf35d8 / Xbox Load_XAnimPartTrans.
    private static void Load_XAnimPartTrans(
        XAnimPartTrans trans,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        TraceXAnim(
            context,
            $"Load_XAnimPartTrans begin size={trans.Size} smallTrans={trans.SmallTrans}");
        if (trans.Size == 0)
        {
            trans.Frame0 = context.ReadCurrentStreamObject<XAnimPartTransFrame0>();
            TraceXAnim(context, "Load_XAnimPartTrans frame0");
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimPartTransFrames>();
        int frameCount = trans.Size + 1;

        // PS3 0xed920 loads the inline XAnimDynamicFrames metadata at 1 byte
        // per frame when numFrames <= 255, otherwise 2 bytes per frame.
        // The pointed +0x18 payload then carries the actual translation data
        // at 3 bytes per frame for smallTrans, otherwise 6 bytes per frame.
        frames.DynamicFrameByteCount = GetDynamicIndexByteCount(parts, frameCount);
        frames.DynamicFrames = context.ReadCurrentStreamBytes(frames.DynamicFrameByteCount);
        frames.IndexByteCount = frameCount * (trans.SmallTrans != 0 ? 3 : 6);

        context.ResolvePointerValue(
            frames.Indices,
            CreateRawByteArrayAttribute(
                trans.SmallTrans != 0 ? 1 : 4,
                nameof(XAnimPartTransFrames.IndexByteCount)),
            frames);

        trans.Frames = frames;
        TraceXAnim(
            context,
            $"Load_XAnimPartTrans frames frameCount={frameCount} dynamicFrameBytes={frames.DynamicFrameByteCount} indexBytes={frames.IndexByteCount}");
    }

    // PS3 0xf3f50 -> 0xf3f10 / Xbox Load_XAnimDeltaPartQuat2.
    private static void Load_XAnimDeltaPartQuat2(
        XAnimDeltaPartQuat2 quat,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        TraceXAnim(context, $"Load_XAnimDeltaPartQuat2 begin size={quat.Size}");
        if (quat.Size == 0)
        {
            quat.Frame0 = context.ReadCurrentStreamObject<XQuat2>();
            TraceXAnim(context, "Load_XAnimDeltaPartQuat2 frame0");
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimDeltaPartQuatDataFrames2>();
        frames.FrameCount = quat.Size + 1;
        frames.DynamicIndexByteCount = GetDynamicIndexByteCount(parts, frames.FrameCount);
        frames.DynamicIndices = context.ReadCurrentStreamBytes(frames.DynamicIndexByteCount);
        context.ResolvePointerProperty(frames, nameof(XAnimDeltaPartQuatDataFrames2.Frames));
        quat.Frames = frames;
        TraceXAnim(
            context,
            $"Load_XAnimDeltaPartQuat2 frames frameCount={frames.FrameCount} dynamicIndexBytes={frames.DynamicIndexByteCount}");
    }

    // PS3 0xf43d0 -> 0xf4390 / Xbox Load_XAnimDeltaPartQuat.
    private static void Load_XAnimDeltaPartQuat(
        XAnimDeltaPartQuat quat,
        XAnimParts parts,
        IXAssetReaderContext context)
    {
        TraceXAnim(context, $"Load_XAnimDeltaPartQuat begin size={quat.Size}");
        if (quat.Size == 0)
        {
            quat.Frame0 = context.ReadCurrentStreamObject<XQuat>();
            TraceXAnim(context, "Load_XAnimDeltaPartQuat frame0");
            return;
        }

        var frames = context.ReadCurrentStreamObject<XAnimDeltaPartQuatDataFrames>();
        frames.FrameCount = quat.Size + 1;
        frames.DynamicIndexByteCount = GetDynamicIndexByteCount(parts, frames.FrameCount);
        frames.DynamicIndices = context.ReadCurrentStreamBytes(frames.DynamicIndexByteCount);
        context.ResolvePointerProperty(frames, nameof(XAnimDeltaPartQuatDataFrames.Frames));
        quat.Frames = frames;
        TraceXAnim(
            context,
            $"Load_XAnimDeltaPartQuat frames frameCount={frames.FrameCount} dynamicIndexBytes={frames.DynamicIndexByteCount}");
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

    private static void TraceXAnim(
        IXAssetReaderContext context,
        string message)
    {
        if (!TraceXAnimEnabled)
            return;

        Console.Error.WriteLine(
            $"XAnimTrace: src=0x{context.SourcePosition:X} active={context.ActiveStreamBlock} " +
            $"temp=0x{context.GetStreamPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{context.GetStreamPosition(XFILE_BLOCK.LARGE):X} " +
            $"physical=0x{context.GetStreamPosition(XFILE_BLOCK.PHYSICAL):X} " +
            $"vertex=0x{context.GetStreamPosition(XFILE_BLOCK.VERTEX):X} {message}");
    }
}
