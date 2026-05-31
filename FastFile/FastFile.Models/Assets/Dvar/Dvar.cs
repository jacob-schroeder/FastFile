using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Dvar;

public class Dvar()
{
    public ZonePointer<string> name; //0x0
#if !XBOX
    public ZonePointer<string> description; //0x4
#endif
    public UInt16 flags; //0x8
    public byte type; //0xA
    public bool modified; //0xB
    public DvarValue current; //0xC
    public DvarValue latched; //0x1C
    public DvarValue reset; //0x2C
    public DvarLimits domain; //0x3C
    public ZonePointer<Dvar> next; //0x44
}