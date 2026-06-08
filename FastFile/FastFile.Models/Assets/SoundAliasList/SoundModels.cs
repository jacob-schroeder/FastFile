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
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
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
    public uint Offset { get; set; }
    public uint Length { get; set; }
}

public class StreamFileNameRaw
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> Dir { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> Name { get; set; }
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
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public int PhysicalDataByteCount { get; set; }
    public byte[] SoundInfoBytes { get; set; } = new byte[10];
    public ushort SeekTableCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<byte[]> SeekTablePtr { get; set; } = new(0);
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.PHYSICAL)]
    public DirectPointer<byte[]> PhysicalDataPtr { get; set; } = new(0);

    public override string? GetDisplayName => Name;
}

public class PrimedSound
{
    public StreamFileName Filename { get; set; } = new();
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
    public byte Exists { get; set; }
    public byte[] Padding { get; set; } = new byte[2];
    public SoundData Sound { get; set; } = new();
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<LoadedSound> LoadedSoundPtr { get; set; } = new(0);
    public StreamFileName StreamFileName { get; set; }
    public SoundFile[] TableRecords { get; set; } = [];
}

public class SndCurve() : BaseAsset(XAssetType.SndCurve)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> FilenamePtr { get; set; }
    public string Filename => FilenamePtr is { IsResolved: true } ? FilenamePtr.Result ?? string.Empty : string.Empty;
    public ushort KnotCount { get; set; }
    public byte[] AlignmentPadding { get; set; } = new byte[2];
    public byte[] KnotBytes { get; set; } = new byte[16 * 2 * 4];

    public override string? GetDisplayName => Filename;
}

public class SndAlias
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> AliasNamePtr { get; set; }
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Result ?? string.Empty : string.Empty;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> Subtitle { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> SecondaryAliasName { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> ChainAliasName { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> MixerGroup { get; set; }
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<SoundFile> SoundFile { get; set; }
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
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<SndCurve> VolumeFalloffCurve { get; set; }
    public float EnvelopMin { get; set; }
    public float EnvelopMax { get; set; }
    public float EnvelopPercentage { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<SpeakerMap> SpeakerMap { get; set; }
}

public class SndAliasList() : BaseAsset(XAssetType.Sound)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> AliasNamePtr { get; set; }
    public string AliasName => AliasNamePtr is { IsResolved: true } ? AliasNamePtr.Result ?? string.Empty : string.Empty;
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<SndAlias> Head { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE, CountMember = nameof(Count))]
    public DirectPointer<SndAlias[]> AliasesPtr { get; set; } = new(0);
    public int Count { get; set; }

    public override string? GetDisplayName => AliasName;
}
