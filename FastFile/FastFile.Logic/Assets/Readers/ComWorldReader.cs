using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class ComWorldReader
{
    private const int MaxPrimaryLightCount = 4096;

    public static ComWorld Read(ref XFileReadContext context)
    {
        var asset = new ComWorld
        {
            Offset = context.Position,
            NamePtr = context.ReadDirectPointer<string>("ComWorld+0x00.Name"),
            IsInUse = context.ReadInt32(),
            PrimaryLightCount = context.ReadInt32(),
            PrimaryLights = context.ReadDirectPointer<ComPrimaryLight[]>("ComWorld+0x0C.PrimaryLights"),
        };

        XFileReadValidator.ValidateCount(
            ref context,
            "ComWorld.PrimaryLightCount",
            asset.PrimaryLightCount,
            0,
            MaxPrimaryLightCount,
            "EBOOT 0x00104cb0 reads primaryLightCount from +0x08 and helper 0x00104c20 consumes count * 0x44 ComPrimaryLight rows.");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ResolvePrimaryLights(ref context, asset.PrimaryLights, asset.PrimaryLightCount);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    private static void ResolvePrimaryLights(
        ref XFileReadContext context,
        ZonePointer<ComPrimaryLight[]> pointer,
        int count)
    {
        context.ResolveInlinePointerNow(pointer, (ref XFileReadContext pointerContext, ZonePointer<ComPrimaryLight[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var lights = new ComPrimaryLight[Math.Max(0, count)];
                    for (var i = 0; i < lights.Length; i++)
                        lights[i] = ReadPrimaryLight(ref valueContext);

                    foreach (var light in lights)
                        GenericReader.ResolveStringPointerNow(ref valueContext, light.DefName);

                    return lights;
                }));
        });
    }

    private static ComPrimaryLight ReadPrimaryLight(ref XFileReadContext context)
    {
        var light = new ComPrimaryLight
        {
            Type = context.ReadByte(),
            CanUseShadowMap = context.ReadByte(),
            Exponent = context.ReadByte(),
            Unused = context.ReadByte(),
            Color = ReadFloatArray(ref context, 3),
            Dir = ReadFloatArray(ref context, 3),
            Origin = ReadFloatArray(ref context, 3),
            Radius = context.ReadFloat(),
            CosHalfFovOuter = context.ReadFloat(),
            CosHalfFovInner = context.ReadFloat(),
            CosHalfFovExpanded = context.ReadFloat(),
            RotationLimit = context.ReadFloat(),
            TranslationLimit = context.ReadFloat(),
            DefName = context.ReadDirectPointer<string>("ComPrimaryLight+0x40.DefName"),
        };

        return light;
    }

    private static float[] ReadFloatArray(ref XFileReadContext context, int count)
    {
        var values = new float[count];
        for (var i = 0; i < values.Length; i++)
            values[i] = context.ReadFloat();

        return values;
    }
}
