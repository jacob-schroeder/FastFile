using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal delegate BaseAsset XAssetReader(ref ZoneReadContext context);

internal static class XAssetReaderRegistry
{
    private static readonly IReadOnlyDictionary<XAssetType, XAssetReader> Readers =
        new Dictionary<XAssetType, XAssetReader>
        {
            [XAssetType.Localize] = LocalizeReader.Read,
            [XAssetType.Fx] = FxReader.Read,
            [XAssetType.Image] = ImageReader.Read,
            [XAssetType.LoadedSound] = LoadedSoundReader.Read,
            [XAssetType.Material] = MaterialReader.Read,
            [XAssetType.MenuFile] = MenufileReader.Read,
            [XAssetType.PhysCollmap] = PhysicsReader.ReadPhysCollmap,
            [XAssetType.PhysPreset] = PhysicsReader.ReadPhysPreset,
            [XAssetType.RawFile] = RawFileReader.Read,
            [XAssetType.SndCurve] = SndCurveReader.Read,
            [XAssetType.Sound] = SoundReader.Read,
            [XAssetType.StringTable] = StringTableReader.Read,
            [XAssetType.StructuredDataDef] = StructuredDataReader.Read,
            [XAssetType.Techset] = TechsetReader.Read,
            [XAssetType.Tracer] = TracerReader.Read,
            [XAssetType.Weapon] = WeaponReader.Read,
            [XAssetType.XModel] = XModelReader.Read,
            [XAssetType.XModelSurfs] = XModelSurfsReader.Read,
        };

    public static bool TryGetReader(XAssetType type, out XAssetReader reader)
    {
        return Readers.TryGetValue(type, out reader!);
    }
}
