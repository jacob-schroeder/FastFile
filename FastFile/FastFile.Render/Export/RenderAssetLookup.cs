using FastFile.Models.Assets.GfxMap;
using FastFile.Models.Assets.Image;
using FastFile.Models.Assets.LightDef;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime.Blocks;

namespace FastFile.Render.Export;

internal sealed class RenderAssetLookup
{
    private readonly BlockStreamState _blocks;
    private readonly Dictionary<XBlockAddress, MaterialAsset> _materialsByAddress = new();
    private readonly Dictionary<XBlockAddress, GfxImageAsset> _imagesByAddress = new();
    private readonly Dictionary<XBlockAddress, MaterialTechniqueSetAsset> _techsetsByAddress = new();

    public RenderAssetLookup(BlockStreamState blocks, FastFileLoad load)
    {
        _blocks = blocks;

        Dictionary<int, XAssetEntry> entriesByIndex = load.XAssetList.Assets.ToDictionary(x => x.Index);
        foreach (XAssetLoadResult result in load.LoadedAssets)
        {
            if (result.Asset is not { } asset)
                continue;

            if (asset is MaterialTechniqueSetAsset techset)
            {
                AddTechset(techset);
                if (entriesByIndex.TryGetValue(result.Index, out XAssetEntry? entry) && entry is not null)
                    AddTechset(techset, entry.AssetPointerCellAddress);
            }
            else if (asset is MaterialAsset material)
            {
                AddMaterial(material);
                if (entriesByIndex.TryGetValue(result.Index, out XAssetEntry? entry) && entry is not null)
                    AddMaterial(material, entry.AssetPointerCellAddress);
                CollectMaterialImages(material);
            }
            else if (asset is LightDefAsset lightDef)
            {
                AddImage(lightDef.Image);
            }
        }

        foreach (GfxWorldAsset gfxWorld in load.LoadedAssets.Select(x => x.Asset).OfType<GfxWorldAsset>())
        {
            CollectGfxWorldMaterials(gfxWorld);
            CollectGfxWorldImages(gfxWorld);
        }
    }

    public int MaterialCount => _materialsByAddress.Count;
    public int ImageCount => _imagesByAddress.Count;
    public int TechsetCount => _techsetsByAddress.Count;

    public MaterialAsset? ResolveMaterial(XPointer<MaterialAsset> pointer)
    {
        if (pointer.PackedAddress is { } cell && _materialsByAddress.TryGetValue(cell, out MaterialAsset? cellMaterial))
            return cellMaterial;

        return ResolveAddress(pointer.Untyped) is { } address && _materialsByAddress.TryGetValue(address, out MaterialAsset? material)
            ? material
            : null;
    }

    public GfxImageAsset? ResolveImage(XPointerReference pointer)
    {
        if (pointer.PackedAddress is { } cell && _imagesByAddress.TryGetValue(cell, out GfxImageAsset? cellImage))
            return cellImage;

        return ResolveAddress(pointer) is { } address && _imagesByAddress.TryGetValue(address, out GfxImageAsset? image)
            ? image
            : null;
    }

    public MaterialTechniqueSetAsset? ResolveTechniqueSet(XPointer<MaterialTechniqueSetAsset> pointer)
    {
        if (pointer.PackedAddress is { } cell && _techsetsByAddress.TryGetValue(cell, out MaterialTechniqueSetAsset? cellTechset))
            return cellTechset;

        return ResolveAddress(pointer.Untyped) is { } address && _techsetsByAddress.TryGetValue(address, out MaterialTechniqueSetAsset? techset)
            ? techset
            : null;
    }

    private void AddMaterial(MaterialAsset? material)
    {
        if (material?.RuntimeAddress is { } address)
            _materialsByAddress.TryAdd(address, material);
    }

    private void AddMaterial(MaterialAsset? material, XBlockAddress address)
    {
        if (material is not null)
            _materialsByAddress.TryAdd(address, material);
    }

    private void AddMaterial(MaterialAsset? material, XBlockAddress? address)
    {
        if (address is { } cellAddress)
            AddMaterial(material, cellAddress);
    }

    private void AddImage(GfxImageAsset? image)
    {
        if (image?.RuntimeAddress is { } address)
            _imagesByAddress.TryAdd(address, image);
    }

    private void AddImage(GfxImageAsset? image, XBlockAddress? pointerCellAddress)
    {
        AddImage(image);
        if (image is not null && pointerCellAddress is { } address)
            _imagesByAddress.TryAdd(address, image);
    }

    private void AddTechset(MaterialTechniqueSetAsset? techset)
    {
        if (techset?.RuntimeAddress is { } address)
            _techsetsByAddress.TryAdd(address, techset);
    }

    private void AddTechset(MaterialTechniqueSetAsset? techset, XBlockAddress? pointerCellAddress)
    {
        AddTechset(techset);
        if (techset is not null && pointerCellAddress is { } address)
            _techsetsByAddress.TryAdd(address, techset);
    }

    private void CollectMaterialImages(MaterialAsset material)
    {
        foreach (MaterialTextureDef texture in material.Textures)
        {
            AddImage(texture.Image, texture.DataPointer.CellAddress);
            AddImage(texture.Water?.Image, texture.Water?.ImagePointer.CellAddress);
        }
    }

    private void CollectGfxWorldMaterials(GfxWorldAsset gfxWorld)
    {
        foreach (MaterialMemory row in gfxWorld.MaterialMemory)
        {
            MaterialAsset? material = row.Material ?? ResolveMaterial(row.MaterialPointer);
            AddMaterial(material, row.MaterialPointer.CellAddress);
            if (material is not null)
                CollectMaterialImages(material);
        }
    }

    private void CollectGfxWorldImages(GfxWorldAsset gfxWorld)
    {
        AddImage(gfxWorld.OutdoorImage);
        AddImage(gfxWorld.WorldDraw.SkyImage);
        AddImage(gfxWorld.WorldDraw.OutdoorImage);

        foreach (GfxSky sky in gfxWorld.Skies)
            AddImage(sky.SkyImage);

        foreach (GfxImageAsset? image in gfxWorld.WorldDraw.ReflectionImages)
            AddImage(image);

        foreach (GfxLightmapArray lightmap in gfxWorld.WorldDraw.Lightmaps)
        {
            AddImage(lightmap.Primary);
            AddImage(lightmap.Secondary);
        }
    }

    private XBlockAddress? ResolveAddress(XPointerReference pointer)
    {
        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset && pointer.ResolutionMode == XPointerResolutionMode.AliasCell)
        {
            if (pointer.PackedAddress is not { } cell)
                return null;

            int aliasedRaw = _blocks.ReadInt32(cell);
            return XPointerCodec.GetType(aliasedRaw) == PointerType.Offset
                ? XPointerCodec.Decode(aliasedRaw)
                : null;
        }

        return pointer.Type == PointerType.Offset
            ? pointer.PackedAddress
            : null;
    }
}
