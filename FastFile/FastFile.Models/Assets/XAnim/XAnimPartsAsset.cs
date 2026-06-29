using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.XAnim;

public sealed class XAnimPartsAsset : BaseAsset
{
    public const int SerializedSize = 0x58;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public ushort DataByteCount { get; init; }
    public ushort DataShortCount { get; init; }
    public ushort DataIntCount { get; init; }
    public ushort RandomDataByteCount { get; init; }
    public ushort RandomDataIntCount { get; init; }
    public ushort NumFrames { get; init; }
    public byte Flags { get; init; }
    public byte DeltaFlags { get; init; }
    public IReadOnlyList<byte> BoneCounts { get; init; } = [];
    public byte BoneNameCount { get; init; }
    public byte NotifyCount { get; init; }
    public byte AssetType { get; init; }
    public byte Pad1F { get; init; }
    public int RandomDataShortCount { get; init; }
    public int IndexCount { get; init; }
    public float Framerate { get; init; }
    public float Frequency { get; init; }
    public XPointer<ushort[]> NamesPointer { get; init; }
    public IReadOnlyList<ushort> Names { get; init; } = [];
    public XPointer<byte[]> DataBytePointer { get; init; }
    public XPointer<short[]> DataShortPointer { get; init; }
    public XPointer<int[]> DataIntPointer { get; init; }
    public XPointer<short[]> RandomDataShortPointer { get; init; }
    public XPointer<byte[]> RandomDataBytePointer { get; init; }
    public XPointer<int[]> RandomDataIntPointer { get; init; }
    public XPointer<object> IndicesPointer { get; init; }
    public XAnimPackedDataStreams PackedDataStreams { get; init; } = new();
    public XAnimFrameIndexStream Indices { get; init; } = new();
    public XPointer<XAnimNotifyInfo[]> NotifyPointer { get; init; }
    public IReadOnlyList<XAnimNotifyInfo> Notify { get; init; } = [];
    public XPointer<XAnimDeltaPart> DeltaPartPointer { get; init; }
    public XAnimDeltaPart? DeltaPart { get; init; }
}

public sealed class XAnimPackedDataStreams
{
    public IReadOnlyList<byte> QuantizedBytes { get; init; } = [];
    public IReadOnlyList<short> QuantizedShorts { get; init; } = [];
    public IReadOnlyList<int> QuantizedInts { get; init; } = [];
    public IReadOnlyList<short> RandomizedQuantizedShorts { get; init; } = [];
    public IReadOnlyList<byte> RandomizedQuantizedBytes { get; init; } = [];
    public IReadOnlyList<int> RandomizedQuantizedInts { get; init; } = [];
}

public sealed class XAnimFrameIndexStream
{
    public IReadOnlyList<ushort> FrameIndices { get; init; } = [];
    public int EncodedByteCount { get; init; }
    public bool IsByteEncoded { get; init; }
}

public sealed record XAnimNotifyInfo(ushort Name, float Time)
{
    public const int SerializedSize = 0x08;
}

public sealed class XAnimDeltaPart
{
    public const int SerializedSize = 0x0c;

    public XPointer<XAnimPartTrans> TransPointer { get; init; }
    public XAnimPartTrans? Trans { get; init; }
    public XPointer<XAnimDeltaPartQuat2> Quat2Pointer { get; init; }
    public XAnimDeltaPartQuat2? Quat2 { get; init; }
    public XPointer<XAnimDeltaPartQuat> QuatPointer { get; init; }
    public XAnimDeltaPartQuat? Quat { get; init; }
}

public sealed class XAnimPartTrans
{
    public const int SerializedSize = 0x04;

    public ushort Size { get; init; }
    public byte SmallTrans { get; init; }
    public byte Pad3 { get; init; }
    public XAnimPartTransFrame0? Frame0 { get; init; }
    public XAnimPartTransFrames? Frames { get; init; }
}

public sealed record XAnimPartTransFrame0(float X, float Y, float Z)
{
    public const int SerializedSize = 0x0c;
}

public sealed class XAnimPartTransFrames
{
    public const int SerializedSize = 0x1c;

    public XAnimVec3 Mins { get; init; } = new(0, 0, 0);
    public XAnimVec3 Size { get; init; } = new(0, 0, 0);
    public XPointer<byte[]> FramesPointer { get; init; }
    public XAnimDynamicFrames DynamicFrames { get; init; } = new();
    public XAnimTransFramePayload FramePayload { get; init; } = new EmptyXAnimTransFramePayload();
}

public sealed record XAnimVec3(float X, float Y, float Z);

public sealed class XAnimDynamicFrames
{
    public IReadOnlyList<ushort> FrameIndices { get; init; } = [];
    public int EncodedByteCount { get; init; }
}

public abstract class XAnimTransFramePayload
{
}

public sealed class EmptyXAnimTransFramePayload : XAnimTransFramePayload
{
}

public sealed class SmallXAnimTransFramePayload : XAnimTransFramePayload
{
    public IReadOnlyList<SmallXAnimTransFrame> Frames { get; init; } = [];
}

public sealed class LargeXAnimTransFramePayload : XAnimTransFramePayload
{
    public IReadOnlyList<LargeXAnimTransFrame> Frames { get; init; } = [];
}

public sealed record SmallXAnimTransFrame(byte X, byte Y, byte Z);

public sealed record LargeXAnimTransFrame(short X, short Y, short Z);

public sealed record XQuat2(short Value0, short Value1)
{
    public const int SerializedSize = 0x04;
}

public sealed record XQuat(short Value0, short Value1, short Value2, short Value3)
{
    public const int SerializedSize = 0x08;
}

public sealed class XAnimDeltaPartQuat2
{
    public const int SerializedSize = 0x04;

    public ushort Size { get; init; }
    public byte Pad2 { get; init; }
    public byte Pad3 { get; init; }
    public XQuat2? Frame0 { get; init; }
    public XAnimDeltaPartQuatDataFrames2? Frames { get; init; }
}

public sealed class XAnimDeltaPartQuatDataFrames2
{
    public const int SerializedSize = 0x04;

    public XPointer<XQuat2[]> FramesPointer { get; init; }
    public int FrameCount { get; init; }
    public int DynamicIndexByteCount { get; init; }
}

public sealed class XAnimDeltaPartQuat
{
    public const int SerializedSize = 0x04;

    public ushort Size { get; init; }
    public byte Pad2 { get; init; }
    public byte Pad3 { get; init; }
    public XQuat? Frame0 { get; init; }
    public XAnimDeltaPartQuatDataFrames? Frames { get; init; }
}

public sealed class XAnimDeltaPartQuatDataFrames
{
    public const int SerializedSize = 0x04;

    public XPointer<XQuat[]> FramesPointer { get; init; }
    public int FrameCount { get; init; }
    public int DynamicIndexByteCount { get; init; }
}
