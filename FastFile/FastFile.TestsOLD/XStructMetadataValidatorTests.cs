using FastFile.LogicOLD.Zone.Validation;
using FastFile.ModelsOLD.Assets.Localize;
using FastFile.ModelsOLD.Assets.Menu;
using FastFile.ModelsOLD.Assets.Menu.Elements;
using FastFile.ModelsOLD.Assets.Menufile;
using FastFile.ModelsOLD.Assets.RawFiles;
using FastFile.ModelsOLD.Assets.StringTables;
using FastFile.ModelsOLD.Assets.StructuredData;
using FastFile.ModelsOLD.Assets.Weapons;
using FastFile.ModelsOLD.Zone.Attributes;

namespace FastFile.TestsOLD;

public sealed class XStructMetadataValidatorTests
{
    [Fact]
    public void AssetMetadataHasNoStructuralErrors()
    {
        var result = XStructMetadataValidator.ValidateAssetMetadata();

        Assert.True(
            result.IsValid,
            string.Join(Environment.NewLine, result.Errors.Select(error => error.ToString())));
    }

    [Fact]
    public void VerifiedStructEvidenceIsDiscoverable()
    {
        Assert.Contains(
            typeof(RawFile).GetCustomAttributes(typeof(XEbootEvidenceAttribute), inherit: false).Cast<XEbootEvidenceAttribute>(),
            evidence => evidence.Address == "0x103ec0" &&
                        evidence.Trace == "eboot/traces/xasset_loader_findings.txt");

        Assert.Contains(
            typeof(LocalizeEntry).GetCustomAttributes(typeof(XEbootEvidenceAttribute), inherit: false).Cast<XEbootEvidenceAttribute>(),
            evidence => evidence.Address == "0x104278" &&
                        evidence.Trace == "eboot/traces/xasset_loader_findings.txt");

        Assert.Contains(
            typeof(StringTable).GetCustomAttributes(typeof(XEbootEvidenceAttribute), inherit: false).Cast<XEbootEvidenceAttribute>(),
            evidence => evidence.Address == "0x103b18" &&
                        evidence.Trace == "eboot/traces/xasset_loader_findings.txt");

        Assert.Contains(
            typeof(StructuredDataDefSet).GetCustomAttributes(typeof(XEbootEvidenceAttribute), inherit: false).Cast<XEbootEvidenceAttribute>(),
            evidence => evidence.Address == "0x103630" &&
                        evidence.Trace == "eboot/traces/structureddata_loader_102e78_103630.txt");

        Assert.Contains(
            typeof(WeaponVariantDef).GetCustomAttributes(typeof(XEbootEvidenceAttribute), inherit: false).Cast<XEbootEvidenceAttribute>(),
            evidence => evidence.Address == "0x1152f8" &&
                        evidence.Trace == "eboot/traces/weapon_loaders_114678_1152f8.txt");
    }

    [Fact]
    public void EvidenceReportSeparatesVerifiedAndUnverifiedStructs()
    {
        var report = XStructMetadataValidator.GetAssetEvidenceReport();

        Assert.Contains(report.Verified, item => item.TypeName == nameof(RawFile));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(LocalizeEntry));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(StringTable));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(StringTableCell));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(StructuredDataDefSet));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(StructuredDataDef));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(MenuList));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(MenuDef));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(ItemDef));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(WeaponVariantDef));
        Assert.Contains(report.Verified, item => item.TypeName == nameof(WeaponDef));
        Assert.Contains(report.Unverified, item => item.TypeName == "Material");
    }

    [Fact]
    public void EvidenceReportCanRenderCrossReference()
    {
        var markdown = XStructMetadataValidator.GetAssetEvidenceReport().ToMarkdown();

        Assert.Contains("## Unverified", markdown);
        Assert.Contains("`Material`", markdown);
        Assert.Contains("0x103ec0", markdown);
        Assert.Contains("0x103b18", markdown);
        Assert.Contains("0x1152f8", markdown);
    }
}
