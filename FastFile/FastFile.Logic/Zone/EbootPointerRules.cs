using FastFile.Models.Data;

namespace FastFile.Logic.Zone;

internal static class EbootPointerRules
{
    private static readonly IReadOnlyDictionary<string, PointerResolutionKind> ProofedRules =
        new Dictionary<string, PointerResolutionKind>(StringComparer.Ordinal)
        {
            ["XAssetList.ScriptStrings"] = PointerResolutionKind.Direct,
            ["XAssetList.Assets"] = PointerResolutionKind.Direct,
            ["XAssetList.ScriptString"] = PointerResolutionKind.Direct,
            ["XAsset.Header"] = PointerResolutionKind.Alias,
            ["XString"] = PointerResolutionKind.Direct,
            ["RawFile.Buffer"] = PointerResolutionKind.Direct,
            ["Material.TechniqueSet"] = PointerResolutionKind.Alias,
            ["Material.TextureTable"] = PointerResolutionKind.Direct,
            ["Material.ConstantTable"] = PointerResolutionKind.Direct,
            ["Material.StateBitTable"] = PointerResolutionKind.Direct,
            ["Material.Info.Name"] = PointerResolutionKind.Direct,
            ["MaterialAssetRef"] = PointerResolutionKind.Alias,
            ["MaterialTextureDef.Image"] = PointerResolutionKind.Alias,
            ["GfxImageAssetRef"] = PointerResolutionKind.Alias,
            ["MenuList.Name"] = PointerResolutionKind.Direct,
            ["MenuList.Menus"] = PointerResolutionKind.Direct,
            ["MenuList.Menus.Element"] = PointerResolutionKind.Alias,
            ["MenuDef.Items"] = PointerResolutionKind.Direct,
            ["MenuDef.ExpressionData"] = PointerResolutionKind.Direct,
            ["Statement"] = PointerResolutionKind.Direct,
            ["Statement.Entries"] = PointerResolutionKind.Direct,
            ["ExpressionSupportingData"] = PointerResolutionKind.Direct,
            ["MenuEventHandlerSet"] = PointerResolutionKind.Direct,
            ["MenuEventHandler.UnconditionalScript"] = PointerResolutionKind.Direct,
            ["ItemKeyHandler"] = PointerResolutionKind.Direct,
            ["Window.Name"] = PointerResolutionKind.Direct,
            ["Window.Group"] = PointerResolutionKind.Direct,
            ["Window.Background"] = PointerResolutionKind.Alias,
            ["Operand.StringVal"] = PointerResolutionKind.Direct,
            ["Operand.Function"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.WeaponDef"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.HideTags"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.XAnims"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.XAnims.Element"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.AccuracyGraphKnots"] = PointerResolutionKind.Direct,
            ["WeaponVariantDef.OriginalAccuracyGraphKnots"] = PointerResolutionKind.Direct,
            ["Weapon.XString"] = PointerResolutionKind.Direct,
            ["Weapon.UShortArray"] = PointerResolutionKind.Direct,
            ["Weapon.FloatArray"] = PointerResolutionKind.Direct,
            ["Weapon.Vec2Array"] = PointerResolutionKind.Direct,
            ["Weapon.XStringArray"] = PointerResolutionKind.Direct,
            ["Weapon.Material"] = PointerResolutionKind.Alias,
            ["Weapon.XModel"] = PointerResolutionKind.Alias,
            ["Weapon.Fx"] = PointerResolutionKind.Alias,
            ["Weapon.PhysCollmap"] = PointerResolutionKind.Alias,
            ["Weapon.Tracer"] = PointerResolutionKind.Alias,
            ["WeaponDef.GunXModel"] = PointerResolutionKind.Direct,
            ["WeaponDef.GunXModel.Element"] = PointerResolutionKind.Alias,
            ["WeaponDef.szXAnimsR"] = PointerResolutionKind.Direct,
            ["WeaponDef.szXAnimsR.Element"] = PointerResolutionKind.Direct,
            ["WeaponDef.szXAnimsL"] = PointerResolutionKind.Direct,
            ["WeaponDef.szXAnimsL.Element"] = PointerResolutionKind.Direct,
            ["WeaponDef.NoteTrackMaps"] = PointerResolutionKind.Direct,
            ["WeaponDef.BounceSound"] = PointerResolutionKind.Direct,
            ["WeaponDef.BounceSound.Element"] = PointerResolutionKind.Direct,
            ["WeaponDef.WorldGunXModel"] = PointerResolutionKind.Direct,
            ["WeaponDef.WorldGunXModel.Element"] = PointerResolutionKind.Alias,
            ["WeaponDef.ParallelBounce"] = PointerResolutionKind.Direct,
            ["WeaponDef.PerpendicularBounce"] = PointerResolutionKind.Direct,
            ["WeaponDef.AccuracyGraphKnots"] = PointerResolutionKind.Direct,
            ["WeaponDef.OriginalAccuracyGraphKnots"] = PointerResolutionKind.Direct,
            ["WeaponDef.LocationDamageMultipliers"] = PointerResolutionKind.Direct,
            ["StringList.Strings"] = PointerResolutionKind.Direct,
            ["StringList.Strings.Element"] = PointerResolutionKind.Direct,
            ["XModelAssetRef"] = PointerResolutionKind.Alias,
            ["XModelAssetRefArray"] = PointerResolutionKind.Direct,
            ["FxEffectAssetRef"] = PointerResolutionKind.Alias,
            ["PhysPresetAssetRef"] = PointerResolutionKind.Alias,
            ["PhysCollmapAssetRef"] = PointerResolutionKind.Alias,
            ["TracerAssetRef"] = PointerResolutionKind.Alias,
            ["XModelSurfsAssetRef"] = PointerResolutionKind.Alias,
        };

    public static PointerResolutionKind Resolve(
        Pointer pointer,
        PointerResolutionKind requestedKind,
        string? fieldPath,
        out string normalizedFieldPath)
    {
        normalizedFieldPath = Normalize(fieldPath);

        if (ProofedRules.TryGetValue(normalizedFieldPath, out var proofedKind))
            return proofedKind;

        var existingFieldPath = Normalize(pointer.FieldPath);
        if (ProofedRules.TryGetValue(existingFieldPath, out var existingProofedKind))
        {
            normalizedFieldPath = existingFieldPath;
            return existingProofedKind;
        }

        return requestedKind;
    }

    public static bool IsProofed(string? fieldPath)
    {
        return ProofedRules.ContainsKey(Normalize(fieldPath));
    }

    private static string Normalize(string? fieldPath)
    {
        if (string.IsNullOrWhiteSpace(fieldPath))
            return string.Empty;

        var value = fieldPath.Trim();
        if (value.StartsWith("XAsset[", StringComparison.Ordinal)
            && value.EndsWith("].Header", StringComparison.Ordinal))
        {
            return "XAsset.Header";
        }

        if (value.StartsWith("XAssetList.ScriptStrings[", StringComparison.Ordinal))
            return "XAssetList.ScriptString";

        if (value.StartsWith("MenuList.Menus[", StringComparison.Ordinal))
            return "MenuList.Menus.Element";

        if (value.StartsWith("WeaponDef.NoteTrackMaps[", StringComparison.Ordinal))
            return "WeaponDef.NoteTrackMaps";

        if (value.StartsWith("WeaponVariantDef.XAnims[", StringComparison.Ordinal))
            return "WeaponVariantDef.XAnims.Element";

        if (value.StartsWith("WeaponDef.szXAnimsR[", StringComparison.Ordinal))
            return "WeaponDef.szXAnimsR.Element";

        if (value.StartsWith("WeaponDef.szXAnimsL[", StringComparison.Ordinal))
            return "WeaponDef.szXAnimsL.Element";

        if (value.StartsWith("WeaponDef.BounceSound[", StringComparison.Ordinal))
            return "WeaponDef.BounceSound.Element";

        if (value.StartsWith("WeaponDef.GunXModel[", StringComparison.Ordinal))
            return "WeaponDef.GunXModel.Element";

        if (value.StartsWith("WeaponDef.WorldGunXModel[", StringComparison.Ordinal))
            return "WeaponDef.WorldGunXModel.Element";

        if (value.StartsWith("StringList.Strings[", StringComparison.Ordinal))
            return "StringList.Strings.Element";

        if (value == "XStringArray"
            || value.StartsWith("XStringArray[", StringComparison.Ordinal))
        {
            return "Weapon.XStringArray";
        }

        if (value.StartsWith("String[][", StringComparison.Ordinal))
            return "StringList.Strings.Element";

        if (value.StartsWith("XModelAssetRefArray[", StringComparison.Ordinal))
            return "XModelAssetRef";

        return value;
    }
}
