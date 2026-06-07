using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class MaterialReader
{
    private static readonly bool TraceMaterial =
        Environment.GetEnvironmentVariable("FASTFILE_TRACE_MATERIAL") is { Length: > 0 } value
        && value != "0";

    public static Material Read(ref XFileReadContext context)
    {
        return Read(ref context, resolveChildrenNow: false);
    }

    private static Material Read(ref XFileReadContext context, bool resolveChildrenNow)
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
            CameraRegion = context.ReadByte(),
            UnknownXStringCount = context.ReadByte()
        };

#if PS3
        material.MaterialPadding = context.ReadByte();

        for (var i = 0; i < material.Ushorts.Length; i++)
            material.Ushorts[i] = context.ReadUInt16();

        material.UshortPadding = context.ReadBytes(2);

        material.UshortArray = context.ReadDirectPointer<ushort[]>("Material.UshortArray");
#endif

        material.TechniqueSet = context.ReadAliasPointer<MaterialTechniqueSet>("Material.TechniqueSet");
        material.TextureTable = context.ReadDirectPointer<MaterialTextureDef[]>("Material.TextureTable");
        material.ConstantTable = context.ReadDirectPointer<MaterialConstantDef[]>("Material.ConstantTable");
        material.StateBitTable = context.ReadDirectPointer<GfxStateBits[]>("Material.StateBitTable");
        material.UnknownXStringArray = context.ReadDirectPointer<ZonePointer<string>[]>("Material.UnknownXStringArray");

        TraceMaterialRoot(material);
        ResolveMaterialChildren(ref context, material, resolveChildrenNow);

        return material;
    }

    private static void ResolveMaterialChildren(
        ref XFileReadContext context,
        Material material,
        bool resolveNow)
    {
        ResolveInBlock(
            ref context,
            material.Info.NamePtr,
            XFILE_BLOCK.LARGE,
            GenericReader.ReadStringPointerValue,
            resolveNow);
#if PS3
        // EBOOT pushes block 2 before loading this table. Official mp_rust keeps the following
        // large-stream texture/image payload in-place, so this cannot be read with the large cursor.
        material.UshortArray.SetResult([]);
#endif

        ResolveInBlock(
            ref context,
            material.TechniqueSet,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<MaterialTechniqueSet> pointer) =>
            {
                var value = pointerContext.ReadPointerValue(pointer, TechsetReader.Read);
                pointer.SetResult(value);
            },
            resolveNow);
        ResolveInBlock(
            ref context,
            material.TextureTable,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<MaterialTextureDef[]> pointer) =>
            {
                var values = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) => ReadMaterialTextureTable(
                        ref valueContext,
                        material.TextureCount,
                        resolveNow));
                pointer.SetResult(values);
            },
            resolveNow);
        ResolveInBlock(
            ref context,
            material.ConstantTable,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<MaterialConstantDef[]> pointer) =>
            {
                var values = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) =>
                        ReadArray(ref valueContext, material.ConstantCount, ReadMaterialConstantDef));
                pointer.SetResult(values);
            },
            resolveNow);
        ResolveInBlock(
            ref context,
            material.StateBitTable,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<GfxStateBits[]> pointer) =>
            {
                var values = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) => ReadGfxStateBitsTable(
                        ref valueContext,
                        material.StateBitsCount,
                        resolveNow));
                pointer.SetResult(values);
            },
            resolveNow);
        ResolveInBlock(
            ref context,
            material.UnknownXStringArray,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<ZonePointer<string>[]> pointer) =>
            {
                var values = pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) => ReadUnknownXStringArray(
                        ref valueContext,
                        material.UnknownXStringCount,
                        resolveNow));
                pointer.SetResult(values);
            },
            resolveNow);
    }

    private static void TraceMaterialRoot(Material material)
    {
        Trace(
            $"root src=0x{material.Offset:X8} tex={material.TextureCount} const={material.ConstantCount} "
            + $"state={material.StateBitsCount} xstr={material.UnknownXStringCount} "
#if PS3
            + $"ushortArray=0x{material.UshortArray.Raw:X8} "
#endif
            + $"tech=0x{material.TechniqueSet.Raw:X8} texPtr=0x{material.TextureTable.Raw:X8} "
            + $"constPtr=0x{material.ConstantTable.Raw:X8} statePtr=0x{material.StateBitTable.Raw:X8} "
            + $"xstrPtr=0x{material.UnknownXStringArray.Raw:X8}");
    }

    private static void ResolveInBlock<T>(
        ref XFileReadContext context,
        ZonePointer<T> pointer,
        XFILE_BLOCK block,
        XFilePointerResolver<T> resolver,
        bool resolveNow,
        XFileStreamAlignment? alignment = null)
    {
        if (resolveNow)
        {
            if (alignment is { } streamAlignment)
                context.ResolvePointerAlignedNowInBlock(pointer, block, streamAlignment, resolver);
            else
                context.ResolvePointerNowInBlock(pointer, block, resolver);
            return;
        }

        if (alignment is { } queuedAlignment)
            context.ResolvePointerAlignedInBlock(pointer, block, queuedAlignment, resolver);
        else
            context.ResolvePointerInBlock(pointer, block, resolver);
    }

    private static ZonePointer<string>[] ReadUnknownXStringArray(
        ref XFileReadContext context,
        int count,
        bool resolveNow)
    {
        var stringPointers = new ZonePointer<string>[count];
        for (var i = 0; i < stringPointers.Length; i++)
            stringPointers[i] = GenericReader.ReadStringPointer(ref context, resolve: !resolveNow);

        if (resolveNow)
        {
            foreach (var pointer in stringPointers)
                GenericReader.ResolveStringPointerNow(ref context, pointer);
        }

        return stringPointers;
    }

    private static void Trace(string message)
    {
        if (TraceMaterial)
            Console.Error.WriteLine($"[material-trace] {message}");
    }

    public static ZonePointer<Material> ReadMaterialPointer(ref XFileReadContext context)
    {
        var pointer = ReadMaterialPointerField(ref context);
        ResolveMaterialPointer(ref context, pointer);
        return pointer;
    }

    public static ZonePointer<Material> ReadMaterialPointerField(ref XFileReadContext context)
    {
        return context.ReadAliasPointer<Material>("MaterialAssetRef");
    }

    public static void ResolveMaterialPointer(
        ref XFileReadContext context,
        ZonePointer<Material> pointer)
    {
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            ReadMaterialPointerValue);
    }

    public static void ResolveMaterialPointerNow(
        ref XFileReadContext context,
        ZonePointer<Material> pointer)
    {
        context.ResolvePointerNowInBlock(pointer, XFILE_BLOCK.TEMP, ReadMaterialPointerValueNow);
    }

    private static void ReadMaterialPointerValue(
        ref XFileReadContext context,
        ZonePointer<Material> pointer)
    {
        var value = context.ReadPointerValue(pointer, Read);
        pointer.SetResult(value);
    }

    private static void ReadMaterialPointerValueNow(
        ref XFileReadContext context,
        ZonePointer<Material> pointer)
    {
        var value = context.ReadPointerValue(
            pointer,
            (ref XFileReadContext valueContext) => Read(ref valueContext, resolveChildrenNow: true));
        pointer.SetResult(value);
    }

    private static MaterialInfo ReadMaterialInfo(ref XFileReadContext context)
    {
        var info = new MaterialInfo
        {
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
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

    private static MaterialTextureDef[] ReadMaterialTextureTable(
        ref XFileReadContext context,
        int count,
        bool resolvePointersNow)
    {
        var values = new MaterialTextureDef[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadMaterialTextureDefField(ref context);

        foreach (var texture in values)
            ResolveMaterialTextureDefPointer(ref context, texture, resolvePointersNow);

        return values;
    }

    private static MaterialTextureDef ReadMaterialTextureDefField(ref XFileReadContext context)
    {
        var texture = new MaterialTextureDef
        {
            NameHash = context.ReadUInt32(),
            NameStart = context.ReadByte(),
            NameEnd = context.ReadByte(),
            SampleState = context.ReadByte(),
            Semantic = (MaterialTextureSemantic)context.ReadByte(),
        };

        var raw = context.ReadInt32();
        texture.Info = new MaterialTextureDefInfo
        {
            Raw = raw,
            Image = context.CreatePointer<GfxImage>(raw, register: false),
            Water = context.CreatePointer<Water>(raw, register: false),
        };

        return texture;
    }

    private static void ResolveMaterialTextureDefPointer(
        ref XFileReadContext context,
        MaterialTextureDef texture,
        bool resolveNow)
    {
        if (texture.Semantic == MaterialTextureSemantic.TS_WATER_MAP)
        {
            context.RegisterPointer(texture.Info.Water, PointerResolutionKind.Direct, "MaterialTextureDef.Water");
            if (resolveNow)
                context.ResolveInlinePointerNow(texture.Info.Water, ReadWaterPointerValueNow);
            else
                context.ResolvePointer(texture.Info.Water, ReadWaterPointerValue);
        }
        else
        {
            context.RegisterPointer(texture.Info.Image, PointerResolutionKind.Alias, "MaterialTextureDef.Image");
            if (resolveNow)
                ImageReader.ResolveImagePointerNow(ref context, texture.Info.Image);
            else
                ImageReader.ResolveImagePointer(ref context, texture.Info.Image);
        }
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

    private static GfxStateBits[] ReadGfxStateBitsTable(
        ref XFileReadContext context,
        int count,
        bool resolveLoadBitsNow)
    {
        var values = new GfxStateBits[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadGfxStateBitsField(ref context);

#if PS3
        foreach (var value in values)
            ResolveGfxStateBitsLoadBits(ref context, value, resolveLoadBitsNow);
#endif

        return values;
    }

    private static GfxStateBits ReadGfxStateBitsField(ref XFileReadContext context)
    {
        var stateBits = new GfxStateBits();
#if XBOX
        for (var i = 0; i < stateBits.LoadBits.Length; i++)
            stateBits.LoadBits[i] = context.ReadInt32();
#elif PS3
        stateBits.LoadBits = context.ReadDirectPointer<int[]>("GfxStateBits.LoadBits");
        stateBits.Unknown = context.ReadInt32();
#endif
        return stateBits;
    }

#if PS3
    private static void ResolveGfxStateBitsLoadBits(
        ref XFileReadContext context,
        GfxStateBits stateBits,
        bool resolveNow)
    {
        ResolveInBlock(
            ref context,
            stateBits.LoadBits,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<int[]> pointer) =>
            {
                var values = new int[2];
                for (var i = 0; i < values.Length; i++)
                    values[i] = pointerContext.ReadInt32();
                pointer.SetResult(values);
            },
            resolveNow);
    }
#endif

    private static Water ReadWater(ref XFileReadContext context)
    {
        return ReadWater(ref context, resolveChildrenNow: false);
    }

    private static Water ReadWater(ref XFileReadContext context, bool resolveChildrenNow)
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
        water.Image = ImageReader.ReadImagePointerField(ref context);

        var sampleCount = water.M * water.N;
        ResolveWaterFloatArray(ref context, water.H0X, sampleCount, resolveChildrenNow);
        ResolveWaterFloatArray(ref context, water.H0Y, sampleCount, resolveChildrenNow);
        ResolveWaterFloatArray(ref context, water.WTerm, sampleCount, resolveChildrenNow);

        if (resolveChildrenNow)
            ImageReader.ResolveImagePointerNow(ref context, water.Image);
        else
            ImageReader.ResolveImagePointer(ref context, water.Image);

        return water;
    }

    private static void ReadWaterPointerValue(ref XFileReadContext context, ZonePointer<Water> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, ReadWater));
    }

    private static void ReadWaterPointerValueNow(ref XFileReadContext context, ZonePointer<Water> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(
            pointer,
            (ref XFileReadContext valueContext) => ReadWater(ref valueContext, resolveChildrenNow: true)));
    }

    private static void ResolveWaterFloatArray(
        ref XFileReadContext context,
        ZonePointer<float[]> pointer,
        int sampleCount,
        bool resolveNow)
    {
        void Resolver(ref XFileReadContext pointerContext, ZonePointer<float[]> valuePointer)
        {
            valuePointer.SetResult(pointerContext.ReadPointerValue(
                valuePointer,
                (ref XFileReadContext valueContext) => ReadFloatArray(ref valueContext, sampleCount)));
        }

        if (resolveNow)
            context.ResolveInlinePointerNow(pointer, Resolver);
        else
            context.ResolvePointer(pointer, Resolver);
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
