using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.ComWorld;

public sealed class ComWorldAsset : BaseAsset
{
    public const int SerializedSize = 0x10;

    public XAssetType Type => XAssetType.ComMap;

    // 0x00: XString. PS3 ComWorld body stores root+0x00 into varXString and calls Load_XString.
    public XPointer<string> NamePointer { get; init; }
    public string? Name { get; init; }

    // 0x04: ComWorld.isInUse. Xbox PDB names this field; PS3 copies it as the root dword between name and primaryLightCount.
    public int IsInUse { get; init; }

    // 0x08: ComWorld.primaryLightCount. PS3 uses this dword as the ComPrimaryLight array count.
    public int PrimaryLightCount { get; init; }

    // 0x0C: ComWorld.primaryLights. PS3 loads an inline ComPrimaryLight[primaryLightCount] payload when non-null.
    public XPointer<ComPrimaryLight[]> PrimaryLightsPointer { get; init; }
    public IReadOnlyList<ComPrimaryLight> PrimaryLights { get; init; } = [];
}

public sealed class ComPrimaryLight
{
    public const int SerializedSize = 0x44;

    public int Offset { get; init; }

    // 0x00..0x03: Xbox-correlated ComPrimaryLight scalar header; PS3 copies these bytes before consumer use.
    public byte Type { get; init; }
    public byte CanUseShadowMap { get; init; }
    public byte Exponent { get; init; }
    public byte Unused { get; init; }

    // 0x04..0x3F: Xbox-correlated light parameters consumed by primary-light cull/shadow/light-grid code.
    public Vec3 Color { get; init; }
    public Vec3 Dir { get; init; }
    public Vec3 Origin { get; init; }
    public float Radius { get; init; }
    public float CosHalfFovOuter { get; init; }
    public float CosHalfFovInner { get; init; }
    public float CosHalfFovExpanded { get; init; }
    public float RotationLimit { get; init; }
    public float TranslationLimit { get; init; }

    // 0x40: XString. PS3 ComPrimaryLight loader sets varXString to row+0x40 and calls Load_XString.
    public XPointer<string> DefNamePointer { get; init; }
    public string? DefName { get; init; }
}
