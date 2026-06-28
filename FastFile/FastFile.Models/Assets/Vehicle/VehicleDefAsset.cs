using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Weapon;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Vehicle;

public sealed class VehicleDefAsset : BaseAsset
{
    public const int SerializedSize = 0x2D0;
    public const int ScriptStringOffset = 0x1D0;
    public const int ScriptStringCount = 4;
    public const int SurfaceSoundOffset = 0x244;
    public const int SurfaceSoundCount = 31;

    public byte[] RootBytes { get; init; } = [];

    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<string> UseHintStringPointer { get; init; }
    public string? UseHintString { get; init; }
    public VehiclePhysDef Phys { get; init; } = new();
    public XPointer<string> TurretWeaponNamePointer { get; init; }
    public string? TurretWeaponName { get; init; }
    public XPointer<WeaponVariantDef> TurretWeaponPointer { get; init; }
    public WeaponVariantDef? TurretWeapon { get; init; }
    public IReadOnlyList<VehicleSoundAliasField> DirectSoundAliases { get; init; } = [];
    public XBlockAddress? ScriptStringsAddress { get; init; }
    public IReadOnlyList<ushort> ScriptStrings { get; init; } = [];
    public XPointer<MaterialAsset> CompassFriendlyIconPointer { get; init; }
    public MaterialAsset? CompassFriendlyIcon { get; init; }
    public XPointer<MaterialAsset> CompassEnemyIconPointer { get; init; }
    public MaterialAsset? CompassEnemyIcon { get; init; }
    public XPointer<string> SurfaceSoundPrefixPointer { get; init; }
    public string? SurfaceSoundPrefix { get; init; }
    public IReadOnlyList<XPointer<string>> SurfaceSoundAliasPointers { get; init; } = [];
    public IReadOnlyList<string?> SurfaceSoundAliases { get; init; } = [];
}

public sealed class VehiclePhysDef
{
    public const int OffsetInVehicleDef = 0x0A8;
    public const int SerializedSize = 0xB4;

    public int PhysicsEnabled { get; init; }
    public XPointer<string> PhysPresetNamePointer { get; init; }
    public string? PhysPresetName { get; init; }
    public XPointer<PhysPresetAsset> PhysPresetPointer { get; init; }
    public PhysPresetAsset? PhysPreset { get; init; }
    public XPointer<string> AccelGraphNamePointer { get; init; }
    public string? AccelGraphName { get; init; }
}

public sealed class PhysPresetAsset : BaseAsset
{
    public const int SerializedSize = 0x2C;

    public byte[] RootBytes { get; init; } = [];
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }
    public XPointer<string> SndAliasPrefixPointer { get; init; }
    public string? SndAliasPrefix { get; init; }
}

public sealed record VehicleSoundAliasField(
    int Offset,
    XPointer<string> Pointer,
    string? Value);
