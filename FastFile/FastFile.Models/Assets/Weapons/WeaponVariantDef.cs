using FastFile.Models.Assets.Weapons.Enums;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponVariantDef() : BaseAsset(XAssetType.Weapon)
{
    public XPointer<string> InternalNamePtr { get; set; } = null!; // Direct
    public XPointer<WeaponDef> WeaponDefPtr { get; set; } = null!; // Direct
    public WeaponDef? WeaponDef => WeaponDefPtr is { IsResolved: true }
        ? WeaponDefPtr.Value
        : null;

    public XPointer<string> DisplayNamePtr { get; set; } = null!; // Direct
    public XPointer<ushort[]> HideTags { get; set; } = null!; // Direct
    public XPointer<XPointer<string>[]> XAnims { get; set; } = null!; //Count = 37, Direct -> ?
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
    public XPointer<string> szAltWeaponName { get; set; } = null!; // Direct
    public UInt32 altWeaponIndex { get; set; }
    public int iAltRaiseTime { get; set; }
    public XPointer<FastFile.Models.Assets.Material.Material> killIcon { get; set; } = null!; // Alias
    public XPointer<FastFile.Models.Assets.Material.Material> dpadIcon { get; set; } = null!; // Alias
    public int unknown8 { get; set; }
    public int iFirstRaiseTime { get; set; }
    public int iDropAmmoMax { get; set; }
    public float adsDofStart { get; set; }
    public float adsDofEnd { get; set; }
    public short accuracyGraphKnotCount { get; set; }
    public short originalAccuracyGraphKnotCount { get; set; }
    public XPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!; // Direct
    public XPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!; // Direct
    public bool motionTracker { get; set; }
    public bool enhanced { get; set; }
    public bool dpadIconShowsAmmo { get; set; }
    public byte DpadIconShowsAmmoPadding { get; set; }

    public const int WeaponAnimCount = 37;
    public const int HideTagCount = 32;




    public string InternalName => InternalNamePtr is { IsResolved: true }
        ? InternalNamePtr.Value ?? string.Empty
        : string.Empty;

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(InternalName)
        ? $"Weapon 0x{Offset:X8}"
        : InternalName;
}
