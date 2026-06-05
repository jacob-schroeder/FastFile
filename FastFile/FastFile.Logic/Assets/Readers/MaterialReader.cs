using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class MaterialReader
{
    public static Material Read(ref XFileReadContext context)
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
            (ref XFileReadContext pointerContext, ZonePointer<ushort[]> pointer) =>
            {
                var values = new ushort[Material.TECHNIQUE_COUNT];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadUInt16();
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "Material.UshortArray");
#endif

        material.TechniqueSet = context.ReadAliasPointer<MaterialTechniqueSet>("Material.TechniqueSet");
        context.ResolvePointerInBlock(
            material.TechniqueSet,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<MaterialTechniqueSet> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, TechsetReader.Read);
                pointer.SetResult(value);
            });
        material.TextureTable = context.ReadPointer<MaterialTextureDef[]>(
            (ref XFileReadContext pointerContext, ZonePointer<MaterialTextureDef[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.TextureCount, ReadMaterialTextureDef);
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "Material.TextureTable");
        material.ConstantTable = context.ReadPointer<MaterialConstantDef[]>(
            (ref XFileReadContext pointerContext, ZonePointer<MaterialConstantDef[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.ConstantCount, ReadMaterialConstantDef);
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "Material.ConstantTable");
        material.StateBitTable = context.ReadPointer<GfxStateBits[]>(
            (ref XFileReadContext pointerContext, ZonePointer<GfxStateBits[]> pointer) =>
            {
                var values = ReadArray(ref pointerContext, material.StateBitsCount, ReadGfxStateBits);
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "Material.StateBitTable");
        material.UnknownXStringArray = context.ReadDirectPointer<ZonePointer<string>[]>("Material.UnknownXStringArray");

        return material;
    }

    public static ZonePointer<Material> ReadMaterialPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<Material>("MaterialAssetRef");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<Material> p) =>
            {
                var value = pointerContext.ReadPointerValue(p, Read);
                p.SetResult(value);
            });
        return pointer;
    }

    private static MaterialInfo ReadMaterialInfo(ref XFileReadContext context)
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

    private static MaterialTextureDef ReadMaterialTextureDef(ref XFileReadContext context)
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
            Image = context.CreatePointer<GfxImage>(raw, register: false),
            Water = context.CreatePointer<Water>(raw, register: false),
        };

        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            context.RegisterPointer(texture.Info.Water, PointerResolutionKind.Direct, "MaterialTextureDef.Water");
            context.ResolvePointer(texture.Info.Water, ReadWaterPointerValue);
        }
        else
        {
            context.RegisterPointer(texture.Info.Image, PointerResolutionKind.Alias, "MaterialTextureDef.Image");
            context.ResolvePointerInBlock(
                texture.Info.Image,
                XFILE_BLOCK.TEMP,
                (ref XFileReadContext pointerContext, ZonePointer<GfxImage> pointer) =>
                {
                    var value = pointerContext.ReadPointerValue(pointer, ImageReader.Read);
                    pointer.SetResult(value);
                });
        }

        return texture;
    }

    private static MaterialConstantDef ReadMaterialConstantDef(ref XFileReadContext context)
    {
        return new MaterialConstantDef
        {
            NameHash = context.ReadInt32(),
            Name = context.ReadString(12),
            Literal = context.ReadVec4(),
        };
    }

    private static GfxStateBits ReadGfxStateBits(ref XFileReadContext context)
    {
        var stateBits = new GfxStateBits();
#if XBOX
        for (var i = 0; i < stateBits.LoadBits.Length; i++)
            stateBits.LoadBits[i] = context.ReadInt32();
#elif PS3
        stateBits.LoadBits = context.ReadPointer<int[]>(
            (ref XFileReadContext pointerContext, ZonePointer<int[]> pointer) =>
            {
                var values = new int[2];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadInt32();
                pointer.SetResult(values);
            },
            PointerResolutionKind.Direct,
            "GfxStateBits.LoadBits");
        stateBits.Unknown = context.ReadInt32();
#endif
        return stateBits;
    }

    private static Water ReadWater(ref XFileReadContext context)
    {
        var water = new Water
        {
            Writable = new WaterWritable { FloatTime = context.ReadFloat() },
            H0X = context.ReadDirectPointer<float[]>("Water.H0X"),
            H0Y = context.ReadDirectPointer<float[]>("Water.H0Y"),
            WTerm = context.ReadDirectPointer<float[]>("Water.WTerm"),
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
        context.ResolvePointer(water.H0X, (ref XFileReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref XFileReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });
        context.ResolvePointer(water.H0Y, (ref XFileReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref XFileReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });
        context.ResolvePointer(water.WTerm, (ref XFileReadContext pointerContext, ZonePointer<float[]> pointer) =>
        {
            pointer.SetResult(pointerContext.ReadPointerValue(
                pointer,
                (ref XFileReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        });

        return water;
    }

    private static void ReadWaterPointerValue(ref XFileReadContext context, ZonePointer<Water> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadWater));
    }

    private static float[] ReadFloatArray(ref XFileReadContext context, int count)
    {
        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();
        return values;
    }
    private static T[] ReadArray<T>(ref XFileReadContext context, int count, XFileValueReader<T> reader)
    {
        var values = new T[count];
        for (var i = 0; i < count; i++)
            values[i] = reader(ref context);
        return values;
    }
}
