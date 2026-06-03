using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets.Writers;

internal static class MaterialWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        WriteMaterial(context, (Material)asset);
    }

    public static void WriteMaterialPointer(ZoneWriterContext context, ZonePointer<Material>? pointer)
    {
        context.WritePointer(pointer, WriteMaterialPointerValue);
    }

    public static void WriteMaterialPointerValue(
        ZoneWriterContext context,
        ZonePointer<Material> pointer)
    {
        if (pointer.Result is { } material)
            WriteMaterial(context, material);
    }

    private static void WriteMaterial(ZoneWriterContext context, Material material)
    {
        WriteMaterialInfo(context, material.Info);
        context.WriteBytes(material.StateBitsEntry);
        context.WriteByte(material.TextureCount);
        context.WriteByte(material.ConstantCount);
        context.WriteByte(material.StateBitsCount);
        context.WriteByte(material.StateFlags);
        context.WriteByte(material.CameraRegion);
#if PS3
        context.WriteByte(0);
        foreach (var value in material.Ushorts)
            context.WriteUInt16(value);
        context.WritePointer(material.UshortArray, WriteUShortArrayPointerValue);
#endif
        context.WritePointer(material.TechniqueSet, WriteTechniqueSetPointerValue);
        context.WritePointer(material.TextureTable, WriteTextureTablePointerValue);
        context.WritePointer(material.ConstantTable, WriteConstantTablePointerValue);
        context.WritePointer(material.StateBitTable, WriteStateBitTablePointerValue);
        context.WritePointerRaw(material.UnknownXStringArray);
    }

    private static void WriteMaterialInfo(ZoneWriterContext context, MaterialInfo info)
    {
        GenericWriter.WriteStringPointer(context, info.NamePtr);
        context.WriteByte(info.GameFlags);
        context.WriteByte(info.SortKey);
        context.WriteByte(info.TextureAtlasRowCount);
        context.WriteByte(info.TextureAtlasColumnCount);
        context.WriteUInt64(info.DrawSurf.Packed);
        context.WriteInt32(info.SurfaceTypeBits);
#if PS3
        context.WriteInt32(info.Padding);
#endif
    }

    private static void WriteUShortArrayPointerValue(
        ZoneWriterContext context,
        ZonePointer<ushort[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            context.WriteUInt16(value);
    }

    private static void WriteTechniqueSetPointerValue(
        ZoneWriterContext context,
        ZonePointer<MaterialTechniqueSet> pointer)
    {
        if (pointer.Result is { } techniqueSet)
            TechsetWriter.Write(context, techniqueSet);
    }

    private static void WriteTextureTablePointerValue(
        ZoneWriterContext context,
        ZonePointer<MaterialTextureDef[]> pointer)
    {
        foreach (var texture in pointer.Result ?? [])
            WriteMaterialTextureDef(context, texture);
    }

    private static void WriteMaterialTextureDef(ZoneWriterContext context, MaterialTextureDef texture)
    {
        context.WriteUInt32(texture.NameHash);
        context.WriteByte(texture.NameStart);
        context.WriteByte(texture.NameEnd);
        context.WriteByte(texture.SampleState);
        context.WriteByte((byte)texture.Semantic);
        context.WriteByte(texture.IsMatureContent);
        context.WriteBytes(texture.Pad);

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
            context.WritePointer(texture.Info.Water, WriteWaterPointerValue);
        else
            context.WritePointer(texture.Info.Image, ImageWriter.WriteImagePointerValue);
    }

    private static void WriteConstantTablePointerValue(
        ZoneWriterContext context,
        ZonePointer<MaterialConstantDef[]> pointer)
    {
        foreach (var constant in pointer.Result ?? [])
            WriteMaterialConstantDef(context, constant);
    }

    private static void WriteMaterialConstantDef(ZoneWriterContext context, MaterialConstantDef constant)
    {
        context.WriteInt32(constant.NameHash);
        var nameBytes = System.Text.Encoding.Latin1.GetBytes(constant.Name ?? string.Empty);
        Span<byte> fixedName = stackalloc byte[12];
        nameBytes.AsSpan(0, Math.Min(nameBytes.Length, fixedName.Length)).CopyTo(fixedName);
        context.WriteBytes(fixedName);
        context.WriteVec4(constant.Literal);
    }

    private static void WriteStateBitTablePointerValue(
        ZoneWriterContext context,
        ZonePointer<GfxStateBits[]> pointer)
    {
        foreach (var stateBits in pointer.Result ?? [])
            WriteGfxStateBits(context, stateBits);
    }

    private static void WriteGfxStateBits(ZoneWriterContext context, GfxStateBits stateBits)
    {
#if XBOX
        foreach (var value in stateBits.LoadBits)
            context.WriteInt32(value);
#elif PS3
        context.WritePointer(stateBits.LoadBits, (pointerContext, pointer) =>
        {
            foreach (var value in pointer.Result ?? [])
                pointerContext.WriteInt32(value);
        });
        context.WriteInt32(stateBits.Unknown);
#endif
    }

    private static void WriteWaterPointerValue(
        ZoneWriterContext context,
        ZonePointer<Water> pointer)
    {
        if (pointer.Result is not { } water)
            return;

        context.WriteFloat(water.Writable.FloatTime);
        context.WritePointer(water.H0X, WriteFloatArrayPointerValue);
        context.WritePointer(water.H0Y, WriteFloatArrayPointerValue);
        context.WritePointer(water.WTerm, WriteFloatArrayPointerValue);
        context.WriteInt32(water.M);
        context.WriteInt32(water.N);
        context.WriteFloat(water.Lx);
        context.WriteFloat(water.Lz);
        context.WriteFloat(water.Gravity);
        context.WriteFloat(water.Windvel);
        foreach (var value in water.Winddir)
            context.WriteFloat(value);
        context.WriteFloat(water.Amplitude);
        foreach (var value in water.CodeConstant)
            context.WriteFloat(value);
        context.WritePointer(water.Image, ImageWriter.WriteImagePointerValue);
    }

    private static void WriteFloatArrayPointerValue(
        ZoneWriterContext context,
        ZonePointer<float[]> pointer)
    {
        foreach (var value in pointer.Result ?? [])
            context.WriteFloat(value);
    }
}
