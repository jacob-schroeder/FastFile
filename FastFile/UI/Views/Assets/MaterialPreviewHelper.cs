using Avalonia.Media;
using FastFile.ModelsOLD.Assets.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using MaterialAsset = FastFile.ModelsOLD.Assets.Material.Material;

namespace UI.Views.Assets;

internal static class MaterialPreviewHelper
{
    private static readonly MaterialTextureSemantic[] SurfaceColorSemantics =
    [
        MaterialTextureSemantic.TS_COLOR_MAP,
        MaterialTextureSemantic.TS_2D,
        MaterialTextureSemantic.TS_FUNCTION,
        MaterialTextureSemantic.TS_DETAIL_MAP,
        MaterialTextureSemantic.TS_SPECULAR_MAP
    ];

    public static MaterialTextureDef[] ResolveTextureTable(MaterialAsset material)
    {
        return material.TextureTable is { IsResolved: true, Value: not null }
            ? material.TextureTable.Value
            : [];
    }

    public static GfxImage? GetTextureImage(MaterialTextureDef texture)
    {
        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            return texture.Info.Water?.Value?.Image?.Value;
        }

        return texture.Info.Image?.Value;
    }

    public static string GetTexturePointerStatus(MaterialTextureDef texture)
    {
        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            return AssetViewFormatters.FormatPointerRaw(texture.Info.Water);
        }

        return AssetViewFormatters.FormatPointerRaw(texture.Info.Image);
    }

    public static MaterialPreviewColor ResolveSurfaceColor(MaterialAsset? material, int salt)
    {
        if (material is null)
        {
            return new MaterialPreviewColor(
                BuildFallbackColor($"surface:{salt}", salt),
                "unresolved material",
                IsDecodedTexture: false);
        }

        var decoded = DecodePreferredSurfaceTexture(material);
        if (TryAverageColor(decoded?.Result, out var color))
        {
            return new MaterialPreviewColor(
                color,
                $"average color from {decoded!.ImageName}",
                IsDecodedTexture: true);
        }

        return new MaterialPreviewColor(
            BuildFallbackColor(material.GetDisplayName ?? string.Empty, salt),
            "fallback color; no decoded texture payload",
            IsDecodedTexture: false);
    }

    private static DecodedMaterialTexture? DecodePreferredSurfaceTexture(MaterialAsset material)
    {
        var textures = ResolveTextureTable(material);
        if (textures.Length == 0)
        {
            return null;
        }

        foreach (var semantic in SurfaceColorSemantics)
        {
            var decoded = DecodeFirstTexture(textures.Where(texture => texture.Semantic == semantic));
            if (decoded is not null)
            {
                return decoded;
            }
        }

        return DecodeFirstTexture(textures);
    }

    private static DecodedMaterialTexture? DecodeFirstTexture(IEnumerable<MaterialTextureDef> textures)
    {
        foreach (var texture in textures)
        {
            var image = GetTextureImage(texture);
            if (image is null)
            {
                continue;
            }

            var decoded = GfxImageDecoder.Decode(image);
            if (decoded.BgraPixels is { Length: > 0 })
            {
                var imageName = string.IsNullOrWhiteSpace(image.Name)
                    ? "(unnamed image)"
                    : image.Name;
                return new DecodedMaterialTexture(imageName, decoded);
            }
        }

        return null;
    }

    private static bool TryAverageColor(ImageDecodeResult? decoded, out Color color)
    {
        color = default;

        if (decoded?.BgraPixels is not { Length: >= 4 } pixels)
        {
            return false;
        }

        long weightedB = 0;
        long weightedG = 0;
        long weightedR = 0;
        long totalAlpha = 0;

        for (var i = 0; i + 3 < pixels.Length; i += 4)
        {
            var alpha = pixels[i + 3];
            if (alpha < 8)
            {
                continue;
            }

            weightedB += pixels[i] * alpha;
            weightedG += pixels[i + 1] * alpha;
            weightedR += pixels[i + 2] * alpha;
            totalAlpha += alpha;
        }

        if (totalAlpha == 0)
        {
            return false;
        }

        color = Color.FromRgb(
            ClampByte(weightedR / totalAlpha),
            ClampByte(weightedG / totalAlpha),
            ClampByte(weightedB / totalAlpha));
        return true;
    }

    private static Color BuildFallbackColor(string key, int salt)
    {
        var hash = 2166136261u;
        foreach (var character in key)
        {
            hash ^= character;
            hash *= 16777619u;
        }

        hash ^= (uint)salt;
        hash *= 16777619u;

        return Color.FromRgb(
            (byte)(80 + (hash & 0x7F)),
            (byte)(80 + ((hash >> 8) & 0x7F)),
            (byte)(80 + ((hash >> 16) & 0x7F)));
    }

    private static byte ClampByte(long value)
    {
        return (byte)Math.Clamp(value, 0, 255);
    }

    private sealed record DecodedMaterialTexture(string ImageName, ImageDecodeResult Result);
}

internal readonly record struct MaterialPreviewColor(
    Color Color,
    string Source,
    bool IsDecodedTexture);
