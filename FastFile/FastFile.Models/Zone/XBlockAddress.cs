namespace FastFile.Models.Zone;

public readonly record struct XBlockAddress(XFileBlockType BlockType, int Offset)
{
    public override string ToString() => $"{BlockType}:0x{Offset:X}";
}
