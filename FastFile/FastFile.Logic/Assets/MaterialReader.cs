using FastFile.Logic.Assets.Generic;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;

namespace FastFile.Logic.Assets;

internal static class MaterialReader
{
    public static Material Read(ref ZoneReadContext context)
    {
        var material = new Material
        {
            Offset = context.Position,
            Info = ReadMaterialInfo(ref context),
            StateBitsEntry = context.ReadBytes(Material.TECHNIQUE_COUNT),
            TextureCount = context.ReadByte(),
            ConstantCount = context.ReadByte(),
            StateBitsCount = context.ReadByte(),
            StateFlags = context.ReadByte(),
            CameraRegion = context.ReadByte()
        };

#if PS3
        context.ReadByte();

        for (var i = 0; i < material.Ushorts.Length; i++)
            material.Ushorts[i] = context.ReadUInt16();

        material.UshortArray = context.ReadPointer<ushort[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<ushort[]> pointer) =>
            {
                var values = new ushort[Material.TECHNIQUE_COUNT];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadUInt16();
                pointer.SetResult(values);
            });
#endif

        material.TechniqueSet = context.ReadPointer<MaterialTechniqueSet>(
            (ref ZoneReadContext pointerContext, ZonePointer<MaterialTechniqueSet> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, TechsetReader.Read);
                pointer.SetResult(value);
            });
        material.TextureTable = context.ReadPointer<MaterialTextureDef[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<MaterialTextureDef[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.TextureCount, ReadMaterialTextureDef);
                pointer.SetResult(values);
            });
        material.ConstantTable = context.ReadPointer<MaterialConstantDef[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<MaterialConstantDef[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.ConstantCount, ReadMaterialConstantDef);
                pointer.SetResult(values);
            });
        material.StateBitTable = context.ReadPointer<GfxStateBits[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<GfxStateBits[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.StateBitsCount, ReadGfxStateBits);
                pointer.SetResult(values);
            });
        material.UnknownXStringArray = context.ReadPointer<ZonePointer<string>[]>();

        return material;
    }

    public static ZonePointer<Material> ReadMaterialPointer(ref ZoneReadContext context)
    {
        var pointer = context.ReadPointer<Material>();
        context.ResolveInlinePointer(pointer, (ref ZoneReadContext pointerContext, ZonePointer<Material> p) =>
        {
            var value = pointerContext.ReadPointerValue(p, Read);
            p.SetResult(value);
        });
        return pointer;
    }

    private static MaterialInfo ReadMaterialInfo(ref ZoneReadContext context)
    {
        var info = new MaterialInfo
        {
            NamePtr = GenericReader.ReadStringPointer(ref context),
            GameFlags = context.ReadByte(),
            SortKey = context.ReadByte(),
            TextureAtlasRowCount = context.ReadByte(),
            TextureAtlasColumnCount = context.ReadByte(),
        };

        var packed = context.ReadUInt64();
        info.DrawSurf = new GfxDrawSurf
        {
            Packed = packed,
            Fields = new GfxDrawSurfFields { Packed = packed },
        };
        info.SurfaceTypeBits = context.ReadInt32();
#if PS3
        info.Padding = context.ReadInt32();
#endif

        return info;
    }

    private static MaterialTextureDef ReadMaterialTextureDef(ref ZoneReadContext context)
    {
        var texture = new MaterialTextureDef
        {
            NameHash = context.ReadUInt32(),
            NameStart = context.ReadByte(),
            NameEnd = context.ReadByte(),
            SampleState = context.ReadByte(),
            Semantic = (MaterialTextureSemantic)context.ReadByte(),
            IsMatureContent = context.ReadByte(),
            Pad = context.ReadBytes(3),
        };

        var raw = context.ReadInt32();
        texture.Info = new MaterialTextureDefInfo
        {
            Raw = raw,
            Image = new ZonePointer<GfxImage>(raw),
            Water = new ZonePointer<Water>(raw),
        };

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
            context.ResolvePointer(texture.Info.Water, ReadWaterPointerValue);

        return texture;
    }

    private static MaterialConstantDef ReadMaterialConstantDef(ref ZoneReadContext context)
    {
        return new MaterialConstantDef
        {
            NameHash = context.ReadInt32(),
            Name = context.ReadString(12),
            Literal = context.ReadVec4(),
        };
    }

    private static GfxStateBits ReadGfxStateBits(ref ZoneReadContext context)
    {
        var stateBits = new GfxStateBits();
#if XBOX
        for (var i = 0; i < stateBits.LoadBits.Length; i++)
            stateBits.LoadBits[i] = context.ReadInt32();
#elif PS3
        stateBits.LoadBits = context.ReadPointer<int[]>(
            (ref ZoneReadContext pointerContext, ZonePointer<int[]> pointer) =>
            {
                var values = new int[2];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadInt32();
                pointer.SetResult(values);
            });
        stateBits.Unknown = context.ReadInt32();
#endif
        return stateBits;
    }

    private static Water ReadWater(ref ZoneReadContext context)
    {
        var water = new Water
        {
            Writable = new WaterWritable { FloatTime = context.ReadFloat() },
            H0X = context.ReadPointer<float[]>(),
            H0Y = context.ReadPointer<float[]>(),
            WTerm = context.ReadPointer<float[]>(),
            M = context.ReadInt32(),
            N = context.ReadInt32(),
            Lx = context.ReadFloat(),
            Lz = context.ReadFloat(),
            Gravity = context.ReadFloat(),
            Windvel = context.ReadFloat(),
        };

        for (var i = 0; i < water.Winddir.Length; i++)
            water.Winddir[i] = context.ReadFloat();
        water.Amplitude = context.ReadFloat();
        for (var i = 0; i < water.CodeConstant.Length; i++)
            water.CodeConstant[i] = context.ReadFloat();
        water.Image = ImageReader.ReadImagePointer(ref context);

        var sampleCount = water.M * water.N;
        context.ResolvePointer(water.H0X, (ref ZoneReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });
        context.ResolvePointer(water.H0Y, (ref ZoneReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });
        context.ResolvePointer(water.WTerm, (ref ZoneReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref ZoneReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });

        return water;
    }

    private static void ReadWaterPointerValue(ref ZoneReadContext context, ZonePointer<Water> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadWater));
    }

    private static float[] ReadFloatArray(ref ZoneReadContext context, int count)
    {
        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();
        return values;
    }
    private static T[] ReadArray<T>(ref ZoneReadContext context, int count, ZoneValueReader<T> reader)
    {
        var values = new T[count];
        for (var i = 0; i < count; i++)
            values[i] = reader(ref context);
        return values;
    }
}
