using FastFile.Models.Assets.Weapons.Enums;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Weapons;

public sealed class WeaponVariantDef() : BaseAsset(XAssetType.Weapon)
{
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> InternalNamePtr { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct)]
    public DirectPointer<WeaponDef> WeaponDefPtr { get; set; } = null!;
    public WeaponDef? WeaponDef => WeaponDefPtr is { IsResolved: true }
        ? WeaponDefPtr.Result
        : null;

    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> DisplayNamePtr { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(HideTagCount))]
    public DirectPointer<ushort[]> HideTags { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(WeaponAnimCount))]
    public DirectPointer<ZonePointer<string>[]> XAnims { get; set; } = null!; //Count = 37
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
    [XFilePointer(PointerResolutionKind.Direct, Block = XFILE_BLOCK.LARGE)]
    public DirectPointer<string> szAltWeaponName { get; set; } = null!;
    public UInt32 altWeaponIndex { get; set; }
    public int iAltRaiseTime { get; set; }
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<FastFile.Models.Assets.Material.Material> killIcon { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Alias, Block = XFILE_BLOCK.TEMP)]
    public AliasPointer<FastFile.Models.Assets.Material.Material> dpadIcon { get; set; } = null!;
    public int unknown8 { get; set; }
    public int iFirstRaiseTime { get; set; }
    public int iDropAmmoMax { get; set; }
    public float adsDofStart { get; set; }
    public float adsDofEnd { get; set; }
    public short accuracyGraphKnotCount { get; set; }
    public short originalAccuracyGraphKnotCount { get; set; }
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(accuracyGraphKnotCount))]
    public DirectPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!;
    [XFilePointer(PointerResolutionKind.Direct, CountMember = nameof(originalAccuracyGraphKnotCount))]
    public DirectPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!;
    public bool motionTracker { get; set; }
    public bool enhanced { get; set; }
    public bool dpadIconShowsAmmo { get; set; }
    public byte DpadIconShowsAmmoPadding { get; set; }

    public const int WeaponAnimCount = 37;
    public const int HideTagCount = 32;




    public string InternalName => InternalNamePtr is { IsResolved: true }
        ? InternalNamePtr.Result ?? string.Empty
        : string.Empty;

    public override string? GetDisplayName => string.IsNullOrWhiteSpace(InternalName)
        ? $"Weapon 0x{Offset:X8}"
        : InternalName;
}
