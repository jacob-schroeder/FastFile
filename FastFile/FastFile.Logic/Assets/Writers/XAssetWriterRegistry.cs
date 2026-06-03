using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Writers;

internal delegate void XAssetWriter(ZoneWriterContext context, BaseAsset asset);

internal static class XAssetWriterRegistry
{
    private static readonly IReadOnlyDictionary<XAssetType, XAssetWriter> Writers =
        new Dictionary<XAssetType, XAssetWriter>
        {
            [XAssetType.Fx] = FxWriter.Write,
            [XAssetType.Localize] = LocalizeWriter.Write,
            [XAssetType.Image] = ImageWriter.Write,
            [XAssetType.Material] = MaterialWriter.Write,
            [XAssetType.MenuFile] = MenufileWriter.Write,
            [XAssetType.PhysCollmap] = PhysicsWriter.WritePhysCollmap,
            [XAssetType.PhysPreset] = PhysicsWriter.WritePhysPreset,
            [XAssetType.RawFile] = RawFileWriter.Write,
            [XAssetType.StringTable] = StringTableWriter.Write,
            [XAssetType.StructuredDataDef] = StructuredDataWriter.Write,
            [XAssetType.Techset] = TechsetWriter.Write,
            [XAssetType.Tracer] = TracerWriter.Write,
            [XAssetType.Weapon] = WeaponWriter.Write,
            [XAssetType.XModel] = XModelWriter.Write,
            [XAssetType.XModelSurfs] = XModelSurfsWriter.Write,
        };

    public static bool TryGetWriter(XAssetType type, out XAssetWriter writer)
    {
        return Writers.TryGetValue(type, out writer!);
    }
}
