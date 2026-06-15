using System.Buffers.Binary;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.Vehicle;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;
using MaterialAsset = FastFile.Models.Assets.Material.Material;

namespace FastFile.Logic.Assets.Readers;

public sealed class VehicleAssetReader : XAssetReadHandler
{
    private static readonly XPointerFieldAttribute CStringAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Direct,
        Target = XPointerTarget.CString
    };

    private static readonly XPointerFieldAttribute TempAliasWrapperAttribute = new()
    {
        ResolutionKind = PointerResolutionKind.Alias,
        Target = XPointerTarget.Object,
        PayloadBlock = XFILE_BLOCK.TEMP,
        UseCurrentStream = true,
        Alignment = 4,
        OffsetIsAliasCell = true
    };

    public override bool TryResolveLoadedObjectPointers(
        object value,
        IXAssetReaderContext context)
    {
        if (value is not VehicleDef vehicle)
            return false;

        Load_VehicleDef(vehicle, context);
        return true;
    }

    // PS3 0x115678 / Xbox Load_VehicleDef
    private static void Load_VehicleDef(
        VehicleDef vehicle,
        IXAssetReaderContext context)
    {
        context.WithStreamBlock(XFILE_BLOCK.LARGE, () =>
        {
            Load_XString(vehicle, context, 0x000);
            Load_XString(vehicle, context, 0x008);
            Load_VehiclePhysDef(vehicle, context, 0x0A8);
            Load_XString(vehicle, context, 0x198);
            Load_WeaponVariantDefPtr(vehicle, context, 0x19C);
            Load_SndAliasCustomName(vehicle, context, 0x1B4);
            Load_SndAliasCustomName(vehicle, context, 0x1B8);
            Load_MaterialPtr(vehicle, context, 0x1D8);
            Load_MaterialPtr(vehicle, context, 0x1DC);

            foreach (int offset in VehicleSoundAliasOffsets)
                Load_SndAliasCustomName(vehicle, context, offset);

            Load_XString(vehicle, context, 0x240);

            for (int i = 0; i < 31; i++)
                Load_SndAliasCustomName(vehicle, context, 0x244 + i * 4);
        });
    }

    // PS3 0x107220 / Xbox Load_VehiclePhysDef, embedded at VehicleDef +0xA8.
    private static void Load_VehiclePhysDef(
        VehicleDef vehicle,
        IXAssetReaderContext context,
        int baseOffset)
    {
        Load_XString(vehicle, context, baseOffset + 0x04);

        var physPreset = CreateRootPointer<PhysPreset>(
            vehicle,
            baseOffset + 0x08,
            PointerResolutionKind.Alias);

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(physPreset, TempAliasWrapperAttribute, vehicle);
        });

        Load_XString(vehicle, context, baseOffset + 0x0C);
    }

    // PS3 0x115560 -> 0x1152f8 / Xbox Load_WeaponVariantDefPtr.
    private static void Load_WeaponVariantDefPtr(
        VehicleDef vehicle,
        IXAssetReaderContext context,
        int offset)
    {
        var weapon = CreateRootPointer<WeaponVariantDef>(
            vehicle,
            offset,
            PointerResolutionKind.Alias);

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(weapon, TempAliasWrapperAttribute, vehicle);
        });
    }

    private static void Load_MaterialPtr(
        VehicleDef vehicle,
        IXAssetReaderContext context,
        int offset)
    {
        var material = CreateRootPointer<MaterialAsset>(
            vehicle,
            offset,
            PointerResolutionKind.Alias);

        context.WithStreamBlock(XFILE_BLOCK.TEMP, () =>
        {
            context.ResolvePointerValue(material, TempAliasWrapperAttribute, vehicle);
        });
    }

    private static void Load_XString(
        VehicleDef vehicle,
        IXAssetReaderContext context,
        int offset)
    {
        var pointer = CreateRootPointer<string?>(
            vehicle,
            offset,
            PointerResolutionKind.Direct);

        context.ResolvePointerValue(pointer, CStringAttribute, vehicle);
    }

    private static void Load_SndAliasCustomName(
        VehicleDef vehicle,
        IXAssetReaderContext context,
        int offset)
    {
        var pointer = CreateRootPointer<string>(
            vehicle,
            offset,
            PointerResolutionKind.Direct);

        context.ResolveSndAliasCustomName(pointer);
    }

    private static XPointer<T> CreateRootPointer<T>(
        VehicleDef vehicle,
        int offset,
        PointerResolutionKind resolutionKind)
    {
        int raw = BinaryPrimitives.ReadInt32BigEndian(vehicle.RootBytes.AsSpan(offset, sizeof(int)));

        return XPointerCodec.CreatePointer<T>(
            raw,
            resolutionKind,
            new XBlockAddress(XFILE_BLOCK.TEMP, vehicle.Offset + offset));
    }

    private static readonly int[] VehicleSoundAliasOffsets =
    [
        0x1E8,
        0x1EC,
        0x1F0,
        0x1F4,
        0x1FC,
        0x204,
        0x208,
        0x20C,
        0x210,
        0x218,
        0x220,
        0x228,
        0x230,
        0x238
    ];
}
