using FastFile.Models.Assets.Weapons.Enums;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponVariantDef() : BaseAsset(XAssetType.Weapon)
{
    public ZonePointer<string> InternalNamePtr { get; set; } = null!;
    public ZonePointer<WeaponDef> WeaponDefPtr { get; set; } = null!;
    public WeaponDef? WeaponDef => WeaponDefPtr is { IsResolved: true }
        ? WeaponDefPtr.Result
        : null;

    public ZonePointer<string> DisplayNamePtr { get; set; } = null!;
    public ZonePointer<ushort[]> HideTags { get; set; } = null!;
    public ZonePointer<ZonePointer<string>[]> XAnims { get; set; } = null!; //Count = 37
    public float fAdsZoomFov { get; set; }
    public int iAdsTransInTime { get; set; }
    public int iAdsTransOutTime { get; set; }
    public int iClipSize { get; set; }
    public ImpactType impactType { get; set; }
    public int iFireTime { get; set; }
    public WeaponIconRatioType dpadIconRatio { get; set; }
    public float fPenetrateMultiplier { get; set; }
    public float fAdsViewKickCenterSpeed { get; set; }
    public float fHipViewKickCenterSpeed { get; set; }
    public ZonePointer<string> szAltWeaponName { get; set; } = null!;
    public UInt32 altWeaponIndex { get; set; }
    public int iAltRaiseTime { get; set; }
    public ZonePointer<FastFile.Models.Assets.Material.Material> killIcon { get; set; } = null!;
    public ZonePointer<FastFile.Models.Assets.Material.Material> dpadIcon { get; set; } = null!;
    public int unknown8 { get; set; }
    public int iFirstRaiseTime { get; set; }
    public int iDropAmmoMax { get; set; }
    public float adsDofStart { get; set; }
    public float adsDofEnd { get; set; }
    public short accuracyGraphKnotCount { get; set; }
    public short originalAccuracyGraphKnotCount { get; set; }
    public ZonePointer<Vec2[]> accuracyGraphKnots { get; set; } = null!;
    public ZonePointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!;
    public bool motionTracker { get; set; }
    public bool enhanced { get; set; }
    public bool dpadIconShowsAmmo { get; set; }
    public byte DpadIconShowsAmmoPadding { get; set; }




    public string InternalName => InternalNamePtr is { IsResolved: true }
        ? InternalNamePtr.Result ?? string.Empty
        : string.Empty;

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(InternalName)
        ? $"Weapon 0x{Offset:X8}"
        : InternalName;
}
