using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.SoundAliasList;

public class SpeakerLevels
{
    public int Speaker { get; set; }
    public int NumLevels { get; set; }
    public float[] Levels { get; set; } = new float[2];
}

public class ChannelMap
{
    public int EntryCount { get; set; }
    public SpeakerLevels[] Speakers { get; set; } = new SpeakerLevels[6];
}

public class SpeakerMap
{
    public bool IsDefault { get; set; }
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public ChannelMap[][] ChannelMaps { get; set; } = [];
}

public enum SndAliasType : byte
{
    SAT_UNKNOWN = 0x0,
    SAT_LOADED = 0x1,
    SAT_STREAMED = 0x2,
    SAT_PRIMED = 0x3,
    SAT_COUNT = 0x4,
}

public class StreamFileNamePacked
{
    public ulong Offset { get; set; }
    public ulong Length { get; set; }
}

public class StreamFileNameRaw
{
    public ZonePointer<string> Dir { get; set; }
    public ZonePointer<string> Name { get; set; }
}

public class StreamFileInfo
{
    public StreamFileNameRaw Raw { get; set; }
    public StreamFileNamePacked Packed { get; set; }
}

public class StreamFileName
{
    public ushort IsLocalized { get; set; }
    public ushort FileIndex { get; set; }
    public StreamFileInfo Info { get; set; }
}

public class StreamedSound
{
    public StreamFileName Filename { get; set; }
    public uint TotalMsec { get; set; }
}

public class LoadedSound() : BaseAsset(XAssetType.LoadedSound)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;

    public override string? GetDisplayName => Name;
}

public class PrimedSound
{
    public StreamFileName Filename { get; set; }
    public ZonePointer<LoadedSound> LoadedPart { get; set; }
    public int DataOffset { get; set; }
    public int TotalSize { get; set; }
    public uint PrimedCrc { get; set; }
}

public class SoundData
{
    public byte[] Raw { get; set; } = [];
}

public class SoundFile
{
    public SndAliasType Type { get; set; }
    public bool Exists { get; set; }
    public SoundData Sound { get; set; }
}

public class SndCurve() : BaseAsset(XAssetType.SndCurve)
{
    public ZonePointer<string> FilenamePtr { get; set; }
    public string Filename => FilenamePtr is { IsResolved: true } ? FilenamePtr.Result ?? string.Empty : string.Empty;
    public ushort KnotCount { get; set; }

    public override string? GetDisplayName => Filename;
}

public class SndAlias
{
    public ZonePointer<string> AliasNamePtr { get; set; }
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Result ?? string.Empty : string.Empty;
    public ZonePointer<string> Subtitle { get; set; }
    public ZonePointer<string> SecondaryAliasName { get; set; }
    public ZonePointer<string> ChainAliasName { get; set; }
    public ZonePointer<string> MixerGroup { get; set; }
    public ZonePointer<SoundFile> SoundFile { get; set; }
    public int Sequence { get; set; }
    public float VolMin { get; set; }
    public float VolMax { get; set; }
    public float PitchMin { get; set; }
    public float PitchMax { get; set; }
    public float DistMin { get; set; }
    public float DistMax { get; set; }
    public float VelocityMin { get; set; }
    public int Flags { get; set; }
    public float SlavePercentage { get; set; }
    public float Probability { get; set; }
    public float LfePercentage { get; set; }
    public float CenterPercentage { get; set; }
    public int StartDelay { get; set; }
    public ZonePointer<SndCurve> VolumeFalloffCurve { get; set; }
    public float EnvelopMin { get; set; }
    public float EnvelopMax { get; set; }
    public float EnvelopPercentage { get; set; }
    public ZonePointer<SpeakerMap> SpeakerMap { get; set; }
}

public class SndAliasList() : BaseAsset(XAssetType.Sound)
{
    public ZonePointer<string> AliasNamePtr { get; set; }
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Result ?? string.Empty : string.Empty;
    public ZonePointer<SndAlias> Head { get; set; }
    public int Count { get; set; }

    public override string? GetDisplayName => AliasName;
}
