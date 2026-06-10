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
    public byte IsDefault { get; set; }
    public byte[] Padding { get; set; } = new byte[3];
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
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
    public uint Offset { get; set; }
    public uint Length { get; set; }
}

public class StreamFileNameRaw
{
    public XPointer<string> Dir { get; set; } // Direct
    public XPointer<string> Name { get; set; } // Direct
}

public class StreamFileInfo
{
    public StreamFileNameRaw Raw { get; set; } = new();
    public StreamFileNamePacked Packed { get; set; } = new();
}

public class StreamFileName
{
    public uint FileIndex { get; set; }
    public StreamFileInfo Info { get; set; } = new();
}

public class StreamedSound
{
    public StreamFileName Filename { get; set; } = new();
    public uint TotalMsec { get; set; }
}

public class LoadedSound() : BaseAsset(XAssetType.LoadedSound)
{
    public XPointer<string> NamePtr { get; set; } // Direct
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Value ?? string.Empty : string.Empty;
    public int PhysicalDataByteCount { get; set; }
    public byte[] SoundInfoBytes { get; set; } = new byte[10];
    public ushort SeekTableCount { get; set; }
    public XPointer<byte[]> SeekTablePtr { get; set; } // Direct
    public XPointer<byte[]> PhysicalDataPtr { get; set; } // Direct

    public override string? GetDisplayName => Name;
}

public class PrimedSound
{
    public StreamFileName Filename { get; set; } = new();
    public XPointer<LoadedSound> LoadedPart { get; set; } // Unknown?
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
    public byte Exists { get; set; }
    public byte[] Padding { get; set; } = new byte[2];
    public SoundData Sound { get; set; } = new();
    public XPointer<LoadedSound> LoadedSoundPtr { get; set; } // Alias
    public StreamFileName StreamFileName { get; set; }
    public SoundFile[] TableRecords { get; set; } = [];
}

public class SndCurve() : BaseAsset(XAssetType.SndCurve)
{
    public XPointer<string> FilenamePtr { get; set; } // Direct
    public string Filename => FilenamePtr is { IsResolved: true } ? FilenamePtr.Value ?? string.Empty : string.Empty;
    public ushort KnotCount { get; set; }
    public byte[] AlignmentPadding { get; set; } = new byte[2];
    public byte[] KnotBytes { get; set; } = new byte[16 * 2 * 4];

    public override string? GetDisplayName => Filename;
}

public class SndAlias
{
    public XPointer<string> AliasNamePtr { get; set; } // Direct
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Value ?? string.Empty : string.Empty;
    public XPointer<string> Subtitle { get; set; } // Direct
    public XPointer<string> SecondaryAliasName { get; set; } // Direct
    public XPointer<string> ChainAliasName { get; set; } // Direct
    public XPointer<string> MixerGroup { get; set; } // Direct
    public XPointer<SoundFile> SoundFile { get; set; } // Alias
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
    public XPointer<SndCurve> VolumeFalloffCurve { get; set; } // Alias
    public float EnvelopMin { get; set; }
    public float EnvelopMax { get; set; }
    public float EnvelopPercentage { get; set; }
    public XPointer<SpeakerMap> SpeakerMap { get; set; } // Direct
}

public class SndAliasList() : BaseAsset(XAssetType.Sound)
{
    public XPointer<string> AliasNamePtr { get; set; } // Direct
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Value ?? string.Empty : string.Empty;
    public XPointer<SndAlias> Head { get; set; } // Direct
    public XPointer<SndAlias[]> AliasesPtr { get; set; } // Direct
    public int Count { get; set; }

    public override string? GetDisplayName => AliasName;
}
