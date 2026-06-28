using System.Buffers.Binary;
using FastFile.Loaders.Assets.Material;
using FastFile.Loaders.Assets.Weapon;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Vehicle;
using FastFile.Models.Pointers;
using FastFile.Models.Pointers.Enums;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;
using WeaponVariantDef = FastFile.Models.Assets.Weapon.WeaponVariantDef;
using XString = FastFile.Models.Pointers.XPointer<string>;

namespace FastFile.Loaders.Assets.Vehicle;

public sealed class VehicleDefLoader
{
    private const int MaterialSize = 0xA8;
    private const int PhysPresetSize = 0x2C;
    private static readonly int[] PreMaterialSoundAliasOffsets =
    [
        0x1B4,
        0x1B8
    ];

    private static readonly int[] PostMaterialSoundAliasOffsets =
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

    private readonly MaterialLoader _materialLoader = new();
    private readonly WeaponLoader _weaponLoader = new();

    public VehicleDefAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new InvalidDataException($"Top-level Vehicle pointer 0x{pointer.Raw:X8} does not reference inline/insert payload data.");

        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            VehicleDefAsset vehicle = ReadVehicleDef(cursor, rootAddress, context);
            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return vehicle;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private VehicleDefAsset ReadVehicleDef(
        FastFileCursor cursor,
        XBlockAddress expectedRootAddress,
        FastFileLoadContext context)
    {
        int sourceOffset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, VehicleDefAsset.SerializedSize, out XBlockAddress rootAddress);
        if (rootAddress != expectedRootAddress)
            throw new InvalidDataException($"Vehicle pointer patched to {expectedRootAddress}, but root loaded at {rootAddress}.");

        XString namePointer = ReadXStringPointerAt(rootBytes, rootAddress, 0x000);
        XString useHintPointer = ReadXStringPointerAt(rootBytes, rootAddress, 0x008);
        VehiclePhysDef phys = ReadVehiclePhysDefRoot(rootBytes, rootAddress);
        XString turretWeaponNamePointer = ReadXStringPointerAt(rootBytes, rootAddress, 0x198);
        XPointer<WeaponVariantDef> turretWeaponPointer = ReadPointerAt<WeaponVariantDef>(rootBytes, rootAddress, 0x19C, XPointerResolutionMode.AliasCell);
        IReadOnlyList<VehicleSoundAliasField> preMaterialSoundRoots = ReadSoundAliasRoots(rootBytes, rootAddress, PreMaterialSoundAliasOffsets);
        IReadOnlyList<VehicleSoundAliasField> postMaterialSoundRoots = ReadSoundAliasRoots(rootBytes, rootAddress, PostMaterialSoundAliasOffsets);
        XBlockAddress scriptStringsAddress = rootAddress.Add(VehicleDefAsset.ScriptStringOffset);
        XPointer<MaterialAsset> compassFriendlyIconPointer = ReadPointerAt<MaterialAsset>(rootBytes, rootAddress, 0x1D8, XPointerResolutionMode.AliasCell);
        XPointer<MaterialAsset> compassEnemyIconPointer = ReadPointerAt<MaterialAsset>(rootBytes, rootAddress, 0x1DC, XPointerResolutionMode.AliasCell);
        XString surfaceSoundPrefixPointer = ReadXStringPointerAt(rootBytes, rootAddress, 0x240);
        IReadOnlyList<XString> surfaceSoundAliasPointers = ReadEmbeddedSoundAliasRoots(rootBytes, rootAddress, VehicleDefAsset.SurfaceSoundOffset, VehicleDefAsset.SurfaceSoundCount);

        string? name;
        string? useHintString;
        VehiclePhysDef resolvedPhys;
        string? turretWeaponName;
        WeaponVariantDef? turretWeapon;
        IReadOnlyList<VehicleSoundAliasField> preMaterialSoundAliases;
        IReadOnlyList<VehicleSoundAliasField> postMaterialSoundAliases;
        IReadOnlyList<VehicleSoundAliasField> directSoundAliases;
        IReadOnlyList<ushort> scriptStrings;
        MaterialAsset? compassFriendlyIcon;
        MaterialAsset? compassEnemyIcon;
        string? surfaceSoundPrefix;
        IReadOnlyList<string?> surfaceSoundAliases;

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            name = context.PointerReader.LoadXString(cursor, namePointer);
            useHintString = context.PointerReader.LoadXString(cursor, useHintPointer);
            resolvedPhys = ReadVehiclePhysDefChildren(cursor, phys, context);
            turretWeaponName = context.PointerReader.LoadXString(cursor, turretWeaponNamePointer);
            turretWeapon = _weaponLoader.LoadVariantFromPointer(cursor, turretWeaponPointer.Untyped, context);
            preMaterialSoundAliases = ReadSoundAliasFields(cursor, preMaterialSoundRoots, context);
            scriptStrings = ReadScriptStringArray(rootBytes, VehicleDefAsset.ScriptStringOffset, VehicleDefAsset.ScriptStringCount);
            compassFriendlyIcon = ReadMaterialPointer(cursor, compassFriendlyIconPointer.Untyped, context);
            compassEnemyIcon = ReadMaterialPointer(cursor, compassEnemyIconPointer.Untyped, context);
            postMaterialSoundAliases = ReadSoundAliasFields(cursor, postMaterialSoundRoots, context);
            directSoundAliases = preMaterialSoundAliases.Concat(postMaterialSoundAliases).ToArray();
            surfaceSoundPrefix = context.PointerReader.LoadXString(cursor, surfaceSoundPrefixPointer);
            surfaceSoundAliases = ReadSoundAliasCellArray(cursor, surfaceSoundAliasPointers, context);
        }
        finally
        {
            context.Blocks.Pop();
        }

        context.Diagnostics.Trace(
            $"  VehicleDef root source=0x{sourceOffset:X} name=0x{namePointer.Raw:X8} useHint=0x{useHintPointer.Raw:X8} " +
            $"physPreset=0x{phys.PhysPresetPointer.Raw:X8} weapon=0x{turretWeaponPointer.Raw:X8} scriptStrings={scriptStringsAddress} " +
            $"surfaceSounds={rootAddress.Add(VehicleDefAsset.SurfaceSoundOffset)} blocks={context.Blocks.DescribePositions()}");

        return new VehicleDefAsset
        {
            Offset = sourceOffset,
            RootBytes = rootBytes,
            NamePointer = namePointer,
            Name = name,
            UseHintStringPointer = useHintPointer,
            UseHintString = useHintString,
            Phys = resolvedPhys,
            TurretWeaponNamePointer = turretWeaponNamePointer,
            TurretWeaponName = turretWeaponName,
            TurretWeaponPointer = turretWeaponPointer,
            TurretWeapon = turretWeapon,
            DirectSoundAliases = directSoundAliases,
            ScriptStringsAddress = scriptStringsAddress,
            ScriptStrings = scriptStrings,
            CompassFriendlyIconPointer = compassFriendlyIconPointer,
            CompassFriendlyIcon = compassFriendlyIcon,
            CompassEnemyIconPointer = compassEnemyIconPointer,
            CompassEnemyIcon = compassEnemyIcon,
            SurfaceSoundPrefixPointer = surfaceSoundPrefixPointer,
            SurfaceSoundPrefix = surfaceSoundPrefix,
            SurfaceSoundAliasPointers = surfaceSoundAliasPointers,
            SurfaceSoundAliases = surfaceSoundAliases
        };
    }

    private static VehiclePhysDef ReadVehiclePhysDefRoot(
        byte[] vehicleRootBytes,
        XBlockAddress vehicleRootAddress)
    {
        var cursor = new FastFileCursor(
            vehicleRootBytes.AsSpan(VehiclePhysDef.OffsetInVehicleDef, VehiclePhysDef.SerializedSize).ToArray(),
            vehicleRootAddress.Add(VehiclePhysDef.OffsetInVehicleDef));

        int physicsEnabled = cursor.ReadInt32();
        XString physPresetNamePointer = ReadXStringPointer(cursor);
        XPointer<PhysPresetAsset> physPresetPointer = ReadPointer<PhysPresetAsset>(cursor, XPointerResolutionMode.AliasCell);
        XString accelGraphNamePointer = ReadXStringPointer(cursor);

        return new VehiclePhysDef
        {
            PhysicsEnabled = physicsEnabled,
            PhysPresetNamePointer = physPresetNamePointer,
            PhysPresetPointer = physPresetPointer,
            AccelGraphNamePointer = accelGraphNamePointer
        };
    }

    private static VehiclePhysDef ReadVehiclePhysDefChildren(
        FastFileCursor cursor,
        VehiclePhysDef phys,
        FastFileLoadContext context)
    {
        string? physPresetName = context.PointerReader.LoadXString(cursor, phys.PhysPresetNamePointer);
        PhysPresetAsset? physPreset = ReadPhysPresetPointer(cursor, phys.PhysPresetPointer.Untyped, context);
        string? accelGraphName = context.PointerReader.LoadXString(cursor, phys.AccelGraphNamePointer);

        return new VehiclePhysDef
        {
            PhysicsEnabled = phys.PhysicsEnabled,
            PhysPresetNamePointer = phys.PhysPresetNamePointer,
            PhysPresetName = physPresetName,
            PhysPresetPointer = phys.PhysPresetPointer,
            PhysPreset = physPreset,
            AccelGraphNamePointer = phys.AccelGraphNamePointer,
            AccelGraphName = accelGraphName
        };
    }

    private static PhysPresetAsset? ReadPhysPresetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, PhysPresetSize, "PhysPreset"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, PhysPresetSize, "PhysPreset");
            return null;
        }

        if (pointer.Type is not (PointerType.Inline or PointerType.Insert))
            throw new NotSupportedException($"PhysPreset pointer 0x{pointer.Raw:X8} uses unsupported source sentinel {pointer.Type}.");

        int sourceOffset = cursor.Offset;
        XBlockAddress? insertCell = pointer.Type == PointerType.Insert
            ? context.Blocks.AllocateInsertPointerCell()
            : null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress rootAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            byte[] rootBytes = context.Blocks.Load(cursor, PhysPresetSize, out XBlockAddress loadedAddress);
            if (loadedAddress != rootAddress)
                throw new InvalidDataException($"PhysPreset pointer patched to {rootAddress}, but root loaded at {loadedAddress}.");

            var rootCursor = new FastFileCursor(rootBytes, rootAddress);
            XString namePointer = ReadXStringPointer(rootCursor);
            rootCursor.Skip(0x1C - rootCursor.Offset);
            XString sndAliasPrefixPointer = ReadXStringPointer(rootCursor);

            string? name;
            string? sndAliasPrefix;
            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                name = context.PointerReader.LoadXString(cursor, namePointer);
                sndAliasPrefix = context.PointerReader.LoadXString(cursor, sndAliasPrefixPointer);
            }
            finally
            {
                context.Blocks.Pop();
            }

            if (insertCell is { } cell)
                context.Blocks.WriteInt32(cell, XPointerCodec.Encode(rootAddress));

            return new PhysPresetAsset
            {
                Offset = sourceOffset,
                RootBytes = rootBytes,
                NamePointer = namePointer,
                Name = name,
                SndAliasPrefixPointer = sndAliasPrefixPointer,
                SndAliasPrefix = sndAliasPrefix
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private MaterialAsset? ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (ResolveAliasCellOffset(pointer, context, MaterialSize, "Material"))
            return null;

        if (pointer.Type == PointerType.Null)
            return null;

        if (pointer.Type == PointerType.Offset)
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MaterialSize, "Material");
            return null;
        }

        return _materialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private static IReadOnlyList<VehicleSoundAliasField> ReadSoundAliasRoots(
        byte[] rootBytes,
        XBlockAddress rootAddress,
        IReadOnlyList<int> offsets)
    {
        var fields = new VehicleSoundAliasField[offsets.Count];
        for (int i = 0; i < fields.Length; i++)
        {
            int offset = offsets[i];
            fields[i] = new VehicleSoundAliasField(offset, ReadXStringPointerAt(rootBytes, rootAddress, offset), null);
        }

        return fields;
    }

    private static IReadOnlyList<VehicleSoundAliasField> ReadSoundAliasFields(
        FastFileCursor cursor,
        IReadOnlyList<VehicleSoundAliasField> fields,
        FastFileLoadContext context)
    {
        var resolved = new VehicleSoundAliasField[fields.Count];
        for (int i = 0; i < fields.Count; i++)
            resolved[i] = fields[i] with { Value = ReadSoundAliasCell(cursor, fields[i].Pointer, context) };

        return resolved;
    }

    private static string? ReadSoundAliasCell(
        FastFileCursor cursor,
        XString pointer,
        FastFileLoadContext context)
    {
        XPointerReference cellPointer = pointer.Untyped;
        if (cellPointer.Type == PointerType.Offset && cellPointer.PackedAddress is { } address)
            context.Blocks.ValidateMaterializedRange(address, sizeof(int), "snd_alias_list_name cell", cellPointer.Raw);

        if (cellPointer.Type == PointerType.Null || cellPointer.Type == PointerType.Offset)
            return null;

        if (cellPointer.Type is not PointerType.Inline)
            throw new NotSupportedException($"snd_alias_list_name cell 0x{cellPointer.Raw:X8} uses unsupported source sentinel {cellPointer.Type}.");

        // EBOOT 0xfedd8 -> 0x2613b0 -> 0x10b318 -> 0xf3d20 aligns the
        // destination stream before materializing the child XString cell; the
        // serialized source cursor itself remains contiguous.
        context.PointerReader.PatchInlinePointerCell(cellPointer, alignment: 4);
        byte[] nestedCellBytes = context.Blocks.Load(cursor, sizeof(int), out XBlockAddress nestedCellAddress);
        var nestedCellCursor = new FastFileCursor(nestedCellBytes, nestedCellAddress);
        XString nestedStringPointer = ReadXStringPointer(nestedCellCursor);
        return context.PointerReader.LoadXString(cursor, nestedStringPointer);
    }

    private static IReadOnlyList<XString> ReadEmbeddedSoundAliasRoots(
        byte[] rootBytes,
        XBlockAddress rootAddress,
        int offset,
        int count)
    {
        var pointers = new XString[count];
        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadXStringPointerAt(rootBytes, rootAddress, offset + (i * sizeof(int)));

        return pointers;
    }

    private static IReadOnlyList<string?> ReadSoundAliasCellArray(
        FastFileCursor cursor,
        IReadOnlyList<XString> pointers,
        FastFileLoadContext context)
    {
        var values = new string?[pointers.Count];
        for (int i = 0; i < pointers.Count; i++)
            values[i] = ReadSoundAliasCell(cursor, pointers[i], context);

        return values;
    }

    private static IReadOnlyList<ushort> ReadScriptStringArray(
        byte[] rootBytes,
        int offset,
        int count)
    {
        var values = new ushort[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = BinaryPrimitives.ReadUInt16BigEndian(rootBytes.AsSpan(offset + (i * sizeof(ushort)), sizeof(ushort)));

        return values;
    }

    private static XString ReadXStringPointerAt(
        byte[] bytes,
        XBlockAddress baseAddress,
        int offset)
    {
        return ReadPointerAt<string>(bytes, baseAddress, offset, XPointerResolutionMode.Direct);
    }

    private static XString ReadXStringPointer(FastFileCursor cursor)
    {
        return ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static XPointer<T> ReadPointerAt<T>(
        byte[] bytes,
        XBlockAddress baseAddress,
        int offset,
        XPointerResolutionMode mode)
    {
        int raw = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, sizeof(int)));
        return new XPointer<T>(raw, mode, baseAddress.Add(offset));
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        XPointerResolutionMode mode)
    {
        int cellOffset = cursor.Offset;
        return new XPointer<T>(cursor.ReadInt32(), mode, cursor.AddressAt(cellOffset));
    }

    private static bool ResolveAliasCellOffset(
        XPointerReference pointer,
        FastFileLoadContext context,
        int targetByteCount,
        string targetName)
    {
        if (pointer.Type != PointerType.Offset || pointer.ResolutionMode != XPointerResolutionMode.AliasCell)
            return false;

        if (pointer.CellAddress is not { } destinationCell)
            throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} has no destination cell to patch.");

        int aliasedRaw = context.PointerReader.ReadAliasCellRaw(pointer);
        if (aliasedRaw != 0)
        {
            PointerType aliasedType = XPointerCodec.GetType(aliasedRaw);
            if (aliasedType != PointerType.Offset)
                throw new InvalidDataException($"Alias-cell pointer 0x{pointer.Raw:X8} resolved to unresolved sentinel 0x{aliasedRaw:X8} for {targetName}.");

            context.PointerReader.ValidateOffsetPointerRange(
                XPointerReference.FromRaw(aliasedRaw, XPointerResolutionMode.Direct, pointer.PackedAddress),
                targetByteCount,
                targetName);
        }

        context.Blocks.WriteInt32(destinationCell, aliasedRaw);
        return true;
    }

}
