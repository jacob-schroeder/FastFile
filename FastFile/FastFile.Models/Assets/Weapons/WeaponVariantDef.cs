using FastFile.Models.Assets.Weapons.Enums;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Models.Assets.Weapons;

[XStruct(Block = XFILE_BLOCK.LARGE, Size = 0x74)]
public sealed class WeaponVariantDef() : BaseAsset(XAssetType.Weapon)
{
    [XField(Offset = 0x00)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> InternalNamePtr { get; set; } = null!; // Direct

    [XField(Offset = 0x04)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.Object)]
    public XPointer<WeaponDef> WeaponDefPtr { get; set; } = null!; // Direct
    public WeaponDef? WeaponDef => WeaponDefPtr is { IsResolved: true }
        ? WeaponDefPtr.Value
        : null;

    [XField(Offset = 0x08)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> DisplayNamePtr { get; set; } = null!; // Direct

    [XField(Offset = 0x0C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(HideTagCount))]
    public XPointer<ushort[]> HideTags { get; set; } = null!; // Direct

    [XField(Offset = 0x10)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.PointerArray,
        ElementResolutionKind = PointerResolutionKind.Direct,
        ElementTarget = XPointerTarget.CString,
        CountMember = nameof(WeaponAnimCount))]
    public XPointer<XPointer<string>[]> XAnims { get; set; } = null!; //Count = 37, Direct -> ?

    [XField(Offset = 0x14)]
    public float fAdsZoomFov { get; set; }

    [XField(Offset = 0x18)]
    public int iAdsTransInTime { get; set; }

    [XField(Offset = 0x1C)]
    public int iAdsTransOutTime { get; set; }

    [XField(Offset = 0x20)]
    public int iClipSize { get; set; }

    [XField(Offset = 0x24)]
    public ImpactType impactType { get; set; }

    [XField(Offset = 0x28)]
    public int iFireTime { get; set; }

    [XField(Offset = 0x2C)]
    public WeaponIconRatioType dpadIconRatio { get; set; }

    [XField(Offset = 0x30)]
    public float fPenetrateMultiplier { get; set; }

    [XField(Offset = 0x34)]
    public float fAdsViewKickCenterSpeed { get; set; }

    [XField(Offset = 0x38)]
    public float fHipViewKickCenterSpeed { get; set; }

    [XField(Offset = 0x3C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Direct, Target = XPointerTarget.CString)]
    public XPointer<string> szAltWeaponName { get; set; } = null!; // Direct

    [XField(Offset = 0x40)]
    public UInt32 altWeaponIndex { get; set; }

    [XField(Offset = 0x44)]
    public int iAltRaiseTime { get; set; }

    [XField(Offset = 0x48)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FastFile.Models.Assets.Material.Material> killIcon { get; set; } = null!; // Alias

    [XField(Offset = 0x4C)]
    [XPointerField(ResolutionKind = PointerResolutionKind.Alias, Target = XPointerTarget.Object)]
    public XPointer<FastFile.Models.Assets.Material.Material> dpadIcon { get; set; } = null!; // Alias

    [XField(Offset = 0x50)]
    public int unknown8 { get; set; }

    [XField(Offset = 0x54)]
    public int iFirstRaiseTime { get; set; }

    [XField(Offset = 0x58)]
    public int iDropAmmoMax { get; set; }

    [XField(Offset = 0x5C)]
    public float adsDofStart { get; set; }

    [XField(Offset = 0x60)]
    public float adsDofEnd { get; set; }

    [XField(Offset = 0x64)]
    public short accuracyGraphKnotCount { get; set; }

    [XField(Offset = 0x66)]
    public short originalAccuracyGraphKnotCount { get; set; }

    [XField(Offset = 0x68)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(accuracyGraphKnotCount))]
    public XPointer<Vec2[]> accuracyGraphKnots { get; set; } = null!; // Direct

    [XField(Offset = 0x6C)]
    [XPointerField(
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.ObjectArray,
        CountMember = nameof(originalAccuracyGraphKnotCount))]
    public XPointer<Vec2[]> originalAccuracyGraphKnots { get; set; } = null!; // Direct

    [XField(Offset = 0x70)]
    public byte motionTracker { get; set; }

    [XField(Offset = 0x71)]
    public byte enhanced { get; set; }

    [XField(Offset = 0x72)]
    public byte dpadIconShowsAmmo { get; set; }

    [XField(Offset = 0x73)]
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
