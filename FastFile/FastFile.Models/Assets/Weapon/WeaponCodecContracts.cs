using FastFile.Models.Codecs;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Weapon;

public static class WeaponCodecContracts
{
    private const string EvidenceText =
        "PS3 Weapon loader: top-level inline WeaponVariantDef pushes TEMP, aligns runtime block to 4, " +
        "loads 0x74 root, then pushes LARGE for the WeaponDef root, strings, fixed arrays, sound-alias custom cells, " +
        "and reference-only external asset pointers. WeaponDef root is 0x684; validated by patch_mp strict loading.";

    public static readonly XStructCodecContract Variant = new(
        "WeaponVariantDef",
        WeaponVariantDef.SerializedSize,
        [
            new XPointerFieldContract("internalName", 0x00, "XString", XPointerResolutionMode.Direct, XPointerSourceSemantics.RequiredInline, "WeaponVariantDef +0x00: direct XString.", InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("weaponDef", 0x04, "WeaponDef", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x04: direct WeaponDef pointer.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("displayName", 0x08, "XString", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x08: direct XString.", InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("hideTags", 0x0C, "ScriptString[32]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x0c: direct uint16[32].", InlineAlignment: 2, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("animationNames", 0x10, "XString[37]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x10: direct XString pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("killIcon", 0x48, "Material", XPointerResolutionMode.AliasCell, XPointerSourceSemantics.ReferenceOnly, "WeaponVariantDef +0x48: alias-cell Material reference."),
            new XPointerFieldContract("dpadIcon", 0x4C, "Material", XPointerResolutionMode.AliasCell, XPointerSourceSemantics.ReferenceOnly, "WeaponVariantDef +0x4c: alias-cell Material reference."),
            new XPointerFieldContract("accuracyGraphKnots", 0x68, "Vec2[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x68: direct Vec2 array.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("originalAccuracyGraphKnots", 0x6C, "Vec2[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponVariantDef +0x6c: direct Vec2 array.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE)
        ],
        EvidenceText);

    public static readonly XStructCodecContract Definition = new(
        "WeaponDef",
        WeaponDef.SerializedSize,
        [
            new XPointerFieldContract("internalName", 0x000, "XString", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x000: direct XString.", InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("gunModels", 0x004, "XModel*[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x004: direct alias-cell XModel pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("handModel", 0x008, "XModel", XPointerResolutionMode.AliasCell, XPointerSourceSemantics.ReferenceOnly, "WeaponDef +0x008: alias-cell XModel reference."),
            new XPointerFieldContract("rightHandAnimationNames", 0x00C, "XString[37]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x00c: direct XString pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("leftHandAnimationNames", 0x010, "XString[37]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x010: direct XString pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("modeName", 0x014, "XString", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x014: direct XString.", InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("soundMapKeys", 0x018, "ScriptString[16]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef note-track direct uint16 array.", InlineAlignment: 2, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("bounceSound", 0x10C, "snd_alias_list_name[31]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x10c: direct sound-alias custom cell table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("worldGunModels", 0x1D8, "XModel*[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x1d8: direct alias-cell XModel pointer table.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("parallelBounce", 0x444, "float[31]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x444: direct float[31].", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("accuracyGraphKnots", 0x514, "Vec2[]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x514: direct Vec2 array using variant count.", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE),
            new XPointerFieldContract("locationDamageMultipliers", 0x5B4, "float[20]", XPointerResolutionMode.Direct, XPointerSourceSemantics.NullableReferenceOrInline, "WeaponDef +0x5b4: direct float[20].", InlineAlignment: 4, InlineBlock: XFileBlockType.LARGE)
        ],
        EvidenceText);

    public static readonly XAssetCodecContract Asset = new(
        XAssetType.Weapon,
        Variant,
        EvidenceText);

    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        Variant,
        Definition,
        Asset
    ];
}
