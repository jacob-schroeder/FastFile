using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponDef
{
    public int Offset { get; set; }
    public ZonePointer<string> InternalNamePtr { get; set; } = null!;

    public ZonePointer<ZonePointer<XModel>[]> gunXModel { get; set; } = null!; //Count = 16
    public ZonePointer<XModel> handXModel { get; set; } = null!;
    public ZonePointer<ZonePointer<string>[]> szXAnimsR {get; set;} = null!; //Count = 37
    public ZonePointer<ZonePointer<string>[]> szXAnimsL {get; set;} = null!; //Count = 37
    public ZonePointer<string> ModeNamePtr { get; set; } = null!;
    
    public string InternalName => InternalNamePtr is { IsResolved: true }
        ? InternalNamePtr.Result ?? string.Empty
        : string.Empty;
}
