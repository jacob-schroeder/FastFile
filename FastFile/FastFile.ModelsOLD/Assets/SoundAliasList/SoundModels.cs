using System.Buffers.Binary;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.ModelsOLD.Assets.SoundAliasList;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x10)]
public sealed class SpeakerLevels
{
    [XField(Offset = 0x00)]
    public int Speaker { get; set; }

    [XField(Offset = 0x04)]
    public int NumLevels { get; set; }

    [XField(Offset = 0x08, Count = 2)]
    public float[] Levels { get; set; } = new float[2];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x64)]
public sealed class XAudioChannelMap
{
    [XField(Offset = 0x00)]
    public int EntryCount { get; set; }

    [XField(Offset = 0x04)]
    public SpeakerLevels[] Speakers { get; set; } = new SpeakerLevels[6];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0xC8)]
public sealed class SpeakerMapChannel
{
    [XField(Offset = 0x00)]
    public XAudioChannelMap[] Outputs { get; set; } = new XAudioChannelMap[2];
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x198)]
public sealed class SpeakerMap
{
    [XField(Offset = 0x00)]
    public byte IsDefault { get; set; }

    [XField(Offset = 0x01, Count = 3)]
    public byte[] Padding { get; set; } = new byte[3];

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x08)]
    public SpeakerMapChannel[] Channels { get; set; } = new SpeakerMapChannel[2];
}

public enum SndAliasType : byte
{
    SAT_UNKNOWN = 0x0,
    SAT_LOADED = 0x1,
    SAT_STREAMED = 0x2,
    SAT_PRIMED = 0x3,
    SAT_COUNT = 0x4,
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x0C)]
public sealed class StreamedSound
{
    [XField(Offset = 0x00)]
    public uint FileIndex { get; set; }

    [XField(Offset = 0x04, Count = 8)]
    public byte[] RawInfoBytes { get; set; } = new byte[8];

    public XPointer<string>? DirPtr { get; set; }
    public XPointer<string>? NamePtr { get; set; }

    public string Directory => DirPtr is { IsResolved: true } ? DirPtr.Value ?? string.Empty : string.Empty;
    public string Filename => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public bool UsesPackedInfo => FileIndex != 0;
    public uint PackedOffset => BinaryPrimitives.ReadUInt32BigEndian(RawInfoBytes.AsSpan(0, 4));
    public uint PackedLength => BinaryPrimitives.ReadUInt32BigEndian(RawInfoBytes.AsSpan(4, 4));
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x1C)]
public sealed class LoadedSound() : BaseAsset(XAssetType.LoadedSound)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public int PhysicalDataByteCount { get; set; }

    [XField(Offset = 0x08, Count = 10)]
    public byte[] SoundInfoBytes { get; set; } = new byte[10];

    [XField(Offset = 0x12)]
    public ushort SeekTableCount { get; set; }

    public int SeekTableByteCount => checked(SeekTableCount * sizeof(uint));

    [XField(Offset = 0x14)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        CountMember = nameof(SeekTableByteCount))]
    public XPointer<byte[]> SeekTablePtr { get; set; } // Direct

    [XField(Offset = 0x18)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ByteArray,
        CountMember = nameof(PhysicalDataByteCount))]
    public XPointer<byte[]> PhysicalDataPtr { get; set; } // Direct

    public override string? GetDisplayName => Name;
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x10)]
public sealed class SoundFile
{
    public int Offset { get; set; }

    [XField(Offset = 0x00)]
    public SndAliasType Type { get; set; }

    [XField(Offset = 0x01)]
    public byte Exists { get; set; }

    [XField(Offset = 0x02, Count = 2)]
    public byte[] Padding { get; set; } = new byte[2];

    [XField(Offset = 0x04, Count = 12)]
    public byte[] UnionData { get; set; } = new byte[12];

    public XPointer<LoadedSound>? LoadedSoundPtr { get; set; }
    public StreamedSound? StreamedSound { get; set; }

    public int UnionRaw0 => BinaryPrimitives.ReadInt32BigEndian(UnionData.AsSpan(0, 4));
    public int UnionRaw1 => BinaryPrimitives.ReadInt32BigEndian(UnionData.AsSpan(4, 4));
    public int UnionRaw2 => BinaryPrimitives.ReadInt32BigEndian(UnionData.AsSpan(8, 4));
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x88)]
public sealed class SndCurve() : BaseAsset(XAssetType.SndCurve)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> FilenamePtr { get; set; } // Direct
    public string Filename => FilenamePtr is { IsResolved: true } ? FilenamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    public ushort KnotCount { get; set; }

    [XField(Offset = 0x06, Count = 2)]
    public byte[] AlignmentPadding { get; set; } = new byte[2];

    [XField(Offset = 0x08, Count = 16 * 2 * 4)]
    public byte[] KnotBytes { get; set; } = new byte[16 * 2 * 4];

    public override string? GetDisplayName => Filename;
}

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x64)]
public sealed class SndAlias
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> AliasNamePtr { get; set; } // Direct
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> SubtitlePtr { get; set; } // Direct

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> SecondaryAliasNamePtr { get; set; } // Direct

    [XField(Offset = 0x0C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> ChainAliasNamePtr { get; set; } // Direct

    [XField(Offset = 0x10)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> MixerGroupPtr { get; set; } // Direct

    [XField(Offset = 0x14)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(SoundFileCount))]
    public XPointer<SoundFile[]> SoundFiles { get; set; } // Direct

    public int SoundFileCount { get; set; } = 1;

    [XField(Offset = 0x18)]
    public int Sequence { get; set; }

    [XField(Offset = 0x1C)]
    public float VolMin { get; set; }

    [XField(Offset = 0x20)]
    public float VolMax { get; set; }

    [XField(Offset = 0x24)]
    public float PitchMin { get; set; }

    [XField(Offset = 0x28)]
    public float PitchMax { get; set; }

    [XField(Offset = 0x2C)]
    public float DistMin { get; set; }

    [XField(Offset = 0x30)]
    public float DistMax { get; set; }

    [XField(Offset = 0x34)]
    public float VelocityMin { get; set; }

    [XField(Offset = 0x38)]
    public int Flags { get; set; }

    [XField(Offset = 0x3C)]
    public float SlavePercentage { get; set; }

    [XField(Offset = 0x40)]
    public float Probability { get; set; }

    [XField(Offset = 0x44)]
    public float LfePercentage { get; set; }

    [XField(Offset = 0x48)]
    public float CenterPercentage { get; set; }

    [XField(Offset = 0x4C)]
    public int StartDelay { get; set; }

    [XField(Offset = 0x50)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        OffsetIsAliasCell = true)]
    public XPointer<SndCurve> VolumeFalloffCurve { get; set; } // Alias

    [XField(Offset = 0x54)]
    public float EnvelopMin { get; set; }

    [XField(Offset = 0x58)]
    public float EnvelopMax { get; set; }

    [XField(Offset = 0x5C)]
    public float EnvelopPercentage { get; set; }

    [XField(Offset = 0x60)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<SpeakerMap> SpeakerMap { get; set; } // Direct

    public SoundFile? PrimarySoundFile => SoundFiles.Value?.FirstOrDefault();
}

[XStruct(Block = XFILE_BLOCK.TEMP, Size = 0x0C)]
public sealed class SndAliasList() : BaseAsset(XAssetType.Sound)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> AliasNamePtr { get; set; } // Direct
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Value ?? string.Empty : string.Empty;

    [XField(Offset = 0x04)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        UseCurrentStream = true,
        Alignment = 4,
        CountMember = nameof(Count))]
    public XPointer<SndAlias[]> Aliases { get; set; } // Direct

    [XField(Offset = 0x08)]
    public int Count { get; set; }

    public override string? GetDisplayName => AliasName;
}
