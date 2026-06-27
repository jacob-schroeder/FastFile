using FastFile.Models.Pointers;

namespace FastFile.Models.Assets.RawFile;

public sealed class RawFileAsset : BaseAsset
{
    public const int SerializedSize = 0x10;

    public XString NamePointer { get; init; }
    public string? Name { get; init; }
    public int CompressedLen { get; init; }
    public int Len { get; init; }
    public XPointer<byte[]> BufferPointer { get; init; }
    public byte[]? Buffer { get; init; }
    public int BufferLength => CompressedLen != 0 ? CompressedLen : Len + 1;
}
