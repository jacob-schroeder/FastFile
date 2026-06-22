using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Utils;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.XAnim;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = RootSize)]
public sealed class XAnimParts() : BaseAsset(XAssetType.XAnim)
{
    public const int RootSize = 0x58;

    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } = null!;
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public ushort DataByteCount { get; set; }

    [XField(Offset = 0x06)]
    public ushort DataShortCount { get; set; }

    [XField(Offset = 0x08)]
    public ushort DataIntCount { get; set; }

    [XField(Offset = 0x0A)]
    public ushort RandomDataByteCount { get; set; }

    [XField(Offset = 0x0C)]
    public ushort RandomDataIntCount { get; set; }

    [XField(Offset = 0x0E)]
    public ushort NumFrames { get; set; }

    [XField(Offset = 0x10)]
    public byte Flags { get; set; }

    [XField(Offset = 0x11)]
    public byte DeltaFlags { get; set; }

    [XField(Offset = 0x12, Count = 0x0A)]
    public byte[] BoneCounts { get; set; } = new byte[0x0A];

    [XField(Offset = 0x1C)]
    public byte BoneNameCount { get; set; }

    [XField(Offset = 0x1D)]
    public byte NotifyCount { get; set; }

    [XField(Offset = 0x1E)]
    public byte AssetType { get; set; }

    [XField(Offset = 0x1F)]
    public byte Unknown1F { get; set; }

    [XField(Offset = 0x20)]
    public int RandomDataShortCount { get; set; }

    [XField(Offset = 0x24)]
    public int IndexCount { get; set; }

    [XField(Offset = 0x28)]
    public float Framerate { get; set; }

    [XField(Offset = 0x2C)]
    public float Frequency { get; set; }

    [XField(Offset = 0x30)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(BoneNameCount))]
    public XPointer<ushort[]> Names { get; set; } = null!;

    [XField(Offset = 0x34)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(DataByteCount))]
    public XPointer<byte[]> DataByte { get; set; } = null!;

    [XField(Offset = 0x38)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(DataShortCount))]
    public XPointer<short[]> DataShort { get; set; } = null!;

    [XField(Offset = 0x3C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(DataIntCount))]
    public XPointer<int[]> DataInt { get; set; } = null!;

    [XField(Offset = 0x40)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 2,
        CountMember = nameof(RandomDataShortCount))]
    public XPointer<short[]> RandomDataShort { get; set; } = null!;

    [XField(Offset = 0x44)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        UseCurrentStream = true,
        Alignment = 1,
        CountMember = nameof(RandomDataByteCount))]
    public XPointer<byte[]> RandomDataByte { get; set; } = null!;

    [XField(Offset = 0x48)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(RandomDataIntCount))]
    public XPointer<int[]> RandomDataInt { get; set; } = null!;

    [XField(Offset = 0x4C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<object> Indices { get; set; } = null!;
    public XPointer<byte[]>? ByteIndices { get; set; }
    public XPointer<ushort[]>? UShortIndices { get; set; }

    [XField(Offset = 0x50)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(NotifyCount))]
    public XPointer<XAnimNotifyInfo[]> Notify { get; set; } = null!;

    [XField(Offset = 0x54)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<XAnimDeltaPart> DeltaPart { get; set; } = null!;

    public override string? GetDisplayName => Name;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class XAnimNotifyInfo
{
    [XField(Offset = 0x00)]
    public ushort Name { get; set; }

    [XField(Offset = 0x04)]
    public float Time { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class XAnimDeltaPart
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<XAnimPartTrans> Trans { get; set; } = null!;

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<XAnimDeltaPartQuat2> Quat2 { get; set; } = null!;

    [XField(Offset = 0x08)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.Object,
        UseCurrentStream = true,
        Alignment = 4)]
    public XPointer<XAnimDeltaPartQuat> Quat { get; set; } = null!;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XAnimPartTrans
{
    [XField(Offset = 0x00)]
    public ushort Size { get; set; }

    [XField(Offset = 0x02)]
    public byte SmallTrans { get; set; }

    [XField(Offset = 0x03)]
    public byte Pad3 { get; set; }

    public XAnimPartTransFrame0? Frame0 { get; set; }
    public XAnimPartTransFrames? Frames { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x0C)]
public sealed class XAnimPartTransFrame0
{
    [XField(Offset = 0x00)]
    public Vec3 Translation { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x1C)]
public sealed class XAnimPartTransFrames
{
    [XField(Offset = 0x00)]
    public Vec3 Mins { get; set; }

    [XField(Offset = 0x0C)]
    public Vec3 Size { get; set; }

    [XField(Offset = 0x18)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.None)]
    public XPointer<byte[]> Indices { get; set; } = null!;

    public int DynamicFrameByteCount { get; set; }
    public byte[] DynamicFrames { get; set; } = [];
    public int IndexByteCount { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XAnimDeltaPartQuat2
{
    [XField(Offset = 0x00)]
    public ushort Size { get; set; }

    [XField(Offset = 0x02, Count = 0x02)]
    public byte[] Pad2 { get; set; } = new byte[0x02];

    public XQuat2? Frame0 { get; set; }
    public XAnimDeltaPartQuatDataFrames2? Frames { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XAnimDeltaPartQuatDataFrames2
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FrameCount))]
    public XPointer<XQuat2[]> Frames { get; set; } = null!;

    public int FrameCount { get; set; }
    public int DynamicIndexByteCount { get; set; }
    public byte[] DynamicIndices { get; set; } = [];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XQuat2
{
    [XField(Offset = 0x00)]
    public short Value0 { get; set; }

    [XField(Offset = 0x02)]
    public short Value1 { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XAnimDeltaPartQuat
{
    [XField(Offset = 0x00)]
    public ushort Size { get; set; }

    [XField(Offset = 0x02, Count = 0x02)]
    public byte[] Pad2 { get; set; } = new byte[0x02];

    public XQuat? Frame0 { get; set; }
    public XAnimDeltaPartQuatDataFrames? Frames { get; set; }
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x04)]
public sealed class XAnimDeltaPartQuatDataFrames
{
    [XField(Offset = 0x00)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(FrameCount))]
    public XPointer<XQuat[]> Frames { get; set; } = null!;

    public int FrameCount { get; set; }
    public int DynamicIndexByteCount { get; set; }
    public byte[] DynamicIndices { get; set; } = [];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x08)]
public sealed class XQuat
{
    [XField(Offset = 0x00)]
    public short Value0 { get; set; }

    [XField(Offset = 0x02)]
    public short Value1 { get; set; }

    [XField(Offset = 0x04)]
    public short Value2 { get; set; }

    [XField(Offset = 0x06)]
    public short Value3 { get; set; }
}
