using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;

namespace FastFile.ModelsOLD.Assets.Dvar;

public class Dvar()
{
    public XPointer<string> name; //0x0
    public XPointer<string> description; //0x4
    public UInt16 flags; //0x8
    public byte type; //0xA
    public bool modified; //0xB
    public DvarValue current; //0xC
    public DvarValue latched; //0x1C
    public DvarValue reset; //0x2C
    public DvarLimits domain; //0x3C
    public XPointer<Dvar> next; //0x44
}