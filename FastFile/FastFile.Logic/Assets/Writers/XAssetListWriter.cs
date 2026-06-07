using System.Buffers.Binary;
using System.Text;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Assets.XModels;

namespace FastFile.Logic.Zone;

public sealed partial class XFileWriter
{
    private static void WriteXAssetList(XFileWriterContext context, XAssetList value)
    {
        context.WriteInt32(value.ScriptStringCount);
        context.WriteInlinePointerMarker();
        context.WriteInt32(value.AssetCount);
        context.WriteInlinePointerMarker();

        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            var scriptStrings = value.ScriptStringsPtr.Result ?? [];
            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
                WriteScriptStringPointerArray(context, value.ScriptStringsPtr, scriptStrings));

            var assets = value.AssetsPtr.Result ?? [];
            WriteAssetArray(context, value.AssetsPtr, assets);
            WriteInlineAssetBodies(context, assets);
        });
    }

    private static void WriteScriptStringPointerArray(
        XFileWriterContext context,
        ZonePointer<ZonePointer<string?>[]>? pointer,
        IReadOnlyList<ZonePointer<string?>> scriptStrings)
    {
        context.Allocate(
            XFILE_BLOCK.TEMP,
            XFileWriteRules.PointerArrayAlignment,
            () =>
            {
                context.RegisterMaterializedPointerValue(pointer);

                var pendingStrings = new List<ZonePointer<string?>>();
                foreach (var pointer in scriptStrings)
                {
                    if (pointer.Result is null)
                    {
                        context.WriteNullPointer();
                        continue;
                    }

                    context.WriteInlinePointerMarker();
                    pendingStrings.Add(pointer);
                }

                foreach (var pending in pendingStrings)
                {
                    context.RegisterMaterializedPointerValue(pending, GetCStringLength(pending.Result));
                    context.WriteCString(pending.Result);
                }
            });
    }

    private static void WriteAssetArray(
        XFileWriterContext context,
        ZonePointer<XAsset[]>? pointer,
        IReadOnlyList<XAsset> assets)
    {
        context.Allocate(
            XFileWriteRules.RootBlock,
            XFileWriteRules.StructAlignment,
            () =>
            {
                context.RegisterMaterializedPointerValue(pointer);

                foreach (var asset in assets)
                    WriteAssetEntry(context, asset);
            });
    }

    private static void WriteAssetEntry(XFileWriterContext context, XAsset asset)
    {
        context.WriteInt32((int)asset.Type);
        WriteAssetPointer(context, asset.XAssetPtr);
    }

    private static void WriteAssetPointer(
        XFileWriterContext context,
        ZonePointer<BaseAsset>? pointer)
    {
        context.WritePointerRaw(pointer, PointerResolutionKind.Alias, "XAsset.Header");
    }

    private static void WriteInlineAssetBodies(
        XFileWriterContext context,
        IReadOnlyList<XAsset> assets)
    {
        foreach (var asset in assets)
        {
            var pointer = asset.XAssetPtr;
            if (pointer is not { IsInlineData: true, Result: not null })
                continue;

            if (!WriteInlineAssetBody(context, pointer))
                return;
        }
    }

    private static bool WriteInlineAssetBody(
        XFileWriterContext context,
        ZonePointer<BaseAsset> pointer)
    {
        var wroteAsset = false;
        var previousDeferral = context.PushInlineWriteDeferral();
        try
        {
            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.RegisterMaterializedPointerValue(pointer);
                wroteAsset = WriteAssetValue(context, pointer.Result!);
            });
        }
        finally
        {
            context.RestoreInlineWriteDeferral(previousDeferral);
        }

        context.WithStreamBlock(XFILE_BLOCK.LARGE, context.ResolveDeferredInlineWrites);
        return wroteAsset;
    }

    private static void WriteInlineAssetReferenceBody<T>(
        XFileWriterContext context,
        ZonePointer<T> pointer,
        Action<XFileWriterContext, T> writer)
    {
        var previousDeferral = context.PushInlineWriteDeferral();
        try
        {
            context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
            {
                context.RegisterMaterializedPointerValue(pointer);
                writer(context, pointer.Result!);
            });
        }
        finally
        {
            context.RestoreInlineWriteDeferral(previousDeferral);
        }

        context.WithStreamBlock(XFILE_BLOCK.LARGE, context.ResolveDeferredInlineWrites);
    }

    private static bool WriteAssetValue(XFileWriterContext context, BaseAsset asset)
    {
        switch (asset)
        {
            case PhysPreset physPreset:
                WritePhysPreset(context, physPreset);
                return true;
            case PhysCollmap physCollmap:
                WritePhysCollmap(context, physCollmap);
                return true;
            case XModelSurfs xmodelSurfs:
                WriteXModelSurfs(context, xmodelSurfs);
                return true;
            case XModel xmodel:
                WriteXModel(context, xmodel);
                return true;
            case XAnimParts xanim:
                WriteXAnimParts(context, xanim);
                return true;
            case Material material:
                WriteMaterial(context, material);
                return true;
            case MaterialPixelShader pixelShader:
                WriteMaterialPixelShader(context, pixelShader);
                return true;
            case MaterialVertexShader vertexShader:
                WriteMaterialVertexShader(context, vertexShader);
                return true;
            case MaterialTechniqueSet techset:
                WriteTechset(context, techset);
                return true;
            case GfxImage image:
                WriteImage(context, image);
                return true;
            case SndAliasList sound:
                WriteSndAliasList(context, sound);
                return true;
            case SndCurve sndCurve:
                WriteSndCurve(context, sndCurve);
                return true;
            case LoadedSound loadedSound:
                WriteLoadedSound(context, loadedSound);
                return true;
            case MenuList menuList:
                WriteMenuList(context, menuList);
                return true;
            case MenuDef menuDef:
                WriteMenuDef(context, menuDef, windowDynamicFlagCount: 2);
                return true;
            case StringTable stringTable:
                WriteStringTable(context, stringTable);
                return true;
            case StructuredDataDefSet structuredData:
                WriteStructuredDataDefSet(context, structuredData);
                return true;
            case RawFile rawFile:
                WriteRawFile(context, rawFile);
                return true;
            case LocalizeEntry localize:
                WriteLocalizeEntry(context, localize);
                return true;
            case WeaponVariantDef weapon:
                WriteWeaponVariantDef(context, weapon);
                return true;
            case FxEffectDef fx:
                WriteFxEffectDef(context, fx);
                return true;
            case FontAsset font:
                WriteFontAsset(context, font);
                return true;
            case TracerDef tracer:
                WriteTracerDef(context, tracer);
                return true;
            default:
                return false;
        }
    }
}
