namespace FastFile.Models.Data;

public class Pointer
{
    public int Raw { get; private set; }
    public PointerKind Kind { get; private set; }
    public int StreamBlockIndex { get; private set; }
    public int Offset { get; private set; }

    public Pointer(int raw)
    {
        Raw = raw;

        if (raw is 0)
        {
            Kind = PointerKind.Null;
            StreamBlockIndex = 0;
            Offset = 0;
            return;
        }

        if (raw is -1 or -2)
        {
            Kind = PointerKind.Inline;
            StreamBlockIndex = 0;
            Offset = 0;
            return;
        }

        StreamBlockIndex = (Raw >> 28) & 0xF;
        Offset = raw & 0x0FFFFFFF;
        return;
    }

    public void SetOffset(int address)
    {
        Offset = address;
    }
}