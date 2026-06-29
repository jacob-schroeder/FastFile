using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.Sound;

public sealed class SoundAliasListAsset : BaseAsset
{
    public const int SerializedSize = 0x0C;

    public XPointer<string> AliasNamePointer { get; init; }
    public string? AliasName { get; init; }
    public XPointer<SndAlias[]> AliasesPointer { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<SndAlias> Aliases { get; init; } = [];
}

public sealed class SndAlias
{
    public const int SerializedSize = 0x64;

    public int Offset { get; init; }
    public XPointer<string> AliasNamePointer { get; init; }
    public string? AliasName { get; init; }
    public XPointer<string> SubtitlePointer { get; init; }
    public string? Subtitle { get; init; }
    public XPointer<string> SecondaryAliasNamePointer { get; init; }
    public string? SecondaryAliasName { get; init; }
    public XPointer<string> ChainAliasNamePointer { get; init; }
    public string? ChainAliasName { get; init; }
    public XPointer<string> MixerGroupPointer { get; init; }
    public string? MixerGroup { get; init; }
    public XPointer<SoundFile[]> SoundFilesPointer { get; init; }
    public int SoundFileCount { get; init; } = 1;
    public IReadOnlyList<SoundFile> SoundFiles { get; init; } = [];
    public int Sequence { get; init; }
    public float VolumeMin { get; init; }
    public float VolumeMax { get; init; }
    public float PitchMin { get; init; }
    public float PitchMax { get; init; }
    public float DistanceMin { get; init; }
    public float DistanceMax { get; init; }
    public float VelocityMin { get; init; }
    public int Flags { get; init; }
    public float SlavePercentage { get; init; }
    public float Probability { get; init; }
    public float LfePercentage { get; init; }
    public float CenterPercentage { get; init; }
    public int StartDelay { get; init; }
    public XPointer<SndCurve> VolumeFalloffCurvePointer { get; init; }
    public SndCurve? VolumeFalloffCurve { get; init; }
    public float EnvelopMin { get; init; }
    public float EnvelopMax { get; init; }
    public float EnvelopPercentage { get; init; }
    public XPointer<SpeakerMap> SpeakerMapPointer { get; init; }
    public SpeakerMap? SpeakerMap { get; init; }
}

public enum SndAliasType : byte
{
    Unknown = 0,
    Loaded = 1,
    Streamed = 2,
    Primed = 3,
    Count = 4
}

public sealed class SoundFile
{
    public const int SerializedSize = 0x10;

    public int Offset { get; init; }
    public SndAliasType Type { get; init; }
    public byte Exists { get; init; }
    public ushort Padding { get; init; }
    public SoundFilePayload? Payload { get; init; }
    public LoadedSoundFile? Loaded => Payload as LoadedSoundFile;
    public StreamedSound? Streamed => Payload as StreamedSound;
}

public abstract class SoundFilePayload
{
}

public sealed class LoadedSoundFile : SoundFilePayload
{
    public XPointer<LoadedSound> LoadedSoundPointer { get; init; }
    public LoadedSound? LoadedSound { get; init; }
}

public sealed class StreamedSound : SoundFilePayload
{
    public uint FileIndex { get; init; }
    public StreamedSoundSource? Source { get; init; }
    public StreamedSoundFileSource? StreamFile => Source as StreamedSoundFileSource;
    public ExternalStreamedSoundSource? ExternalFile => Source as ExternalStreamedSoundSource;
}

public abstract class StreamedSoundSource
{
}

public sealed class StreamedSoundFileSource : StreamedSoundSource
{
    public int StreamFileOffset { get; init; }
    public int StreamFileLength { get; init; }
}

public sealed class ExternalStreamedSoundSource : StreamedSoundSource
{
    public XPointer<string>? DirectoryPointer { get; init; }
    public string? Directory { get; init; }
    public XPointer<string>? FilenamePointer { get; init; }
    public string? Filename { get; init; }
}

public sealed class LoadedSound : BaseAsset
{
    public const int SerializedSize = 0x1C;

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public int PhysicalDataByteCount { get; init; }
    public ushort FrameCount { get; init; }
    public ushort ChannelCount { get; init; }
    public ushort SampleRate { get; init; }
    public ushort Pad0E { get; init; }
    public ushort Pad10 { get; init; }
    public ushort SeekTableCount { get; init; }
    public int SeekTableByteCount => checked(SeekTableCount * sizeof(uint));
    public XPointer<byte[]> SeekTablePointer { get; init; }
    public byte[]? SeekTable { get; init; }
    public XPointer<byte[]> PhysicalDataPointer { get; init; }
    public byte[]? PhysicalData { get; init; }
}

public sealed class SndCurve : BaseAsset
{
    public const int SerializedSize = 0x88;
    public const int MaxKnotCount = 16;
    public const int KnotSerializedSize = 2 * sizeof(float);
    public const int KnotsByteCount = MaxKnotCount * KnotSerializedSize;

    public XPointer<string> FilenamePointer { get; init; }
    public string? Filename { get; init; }
    public ushort KnotCount { get; init; }
    public ushort Padding { get; init; }
    public IReadOnlyList<SndCurveKnot> Knots { get; init; } = [];
}

public readonly record struct SndCurveKnot(float X, float Y);

public sealed class SpeakerMap
{
    public const int SerializedSize = 0x198;

    public int Offset { get; init; }
    public byte IsDefault { get; init; }
    public byte[] Padding { get; init; } = [];
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<SpeakerMapChannel> Channels { get; init; } = [];
}

public sealed class SpeakerMapChannel
{
    public const int SerializedSize = 0xC8;

    public IReadOnlyList<XAudioChannelMap> Outputs { get; init; } = [];
}

public sealed class XAudioChannelMap
{
    public const int SerializedSize = 0x64;

    public int EntryCount { get; init; }
    public IReadOnlyList<SpeakerLevels> Speakers { get; init; } = [];
}

public sealed class SpeakerLevels
{
    public const int SerializedSize = 0x10;

    public int Speaker { get; init; }
    public int NumLevels { get; init; }
    public float Level0 { get; init; }
    public float Level1 { get; init; }
}
