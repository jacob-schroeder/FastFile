using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class LoadedSoundReader
{
    private static readonly bool TraceLoadedSound =
        Environment.GetEnvironmentVariable("FASTFILE_LOADED_SOUND_TRACE") is { Length: > 0 } value
        && value != "0";

    private static int _traceCount;

    public static LoadedSound Read(ref XFileReadContext context)
    {
        var rootOffset = context.Position;
        var rootStream = context.GetActiveStreamAddress();
        var asset = new LoadedSound
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            PhysicalDataByteCount = context.ReadInt32(),
            SoundInfoBytes = context.ReadBytes(10),
            SeekTableCount = context.ReadUInt16(),
        };

        asset.SeekTablePtr = context.ReadDirectPointer<byte[]>("LoadedSound.SeekTable");
        asset.PhysicalDataPtr = context.ReadDirectPointer<byte[]>("LoadedSound.PhysicalData");

        Trace(
            $"root[{_traceCount++}] src=0x{rootOffset:X8} stream=b{rootStream.BlockIndex}:0x{rootStream.Offset:X8} "
            + $"nameRaw=0x{asset.NamePtr.Raw:X8} physicalBytes={asset.PhysicalDataByteCount} "
            + $"seekCount={asset.SeekTableCount} seekRaw=0x{asset.SeekTablePtr.Raw:X8} "
            + $"physicalRaw=0x{asset.PhysicalDataPtr.Raw:X8}");

        context.ResolvePointerInBlock(asset.NamePtr, XFILE_BLOCK.LARGE, GenericReader.ReadStringPointerValue);

        context.ResolvePointerInBlock(
            asset.SeekTablePtr,
            XFILE_BLOCK.LARGE,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> pointer) =>
            {
                var start = pointerContext.Position;
                pointer.SetResult(pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) =>
                        valueContext.ReadBytes(checked(asset.SeekTableCount * 4))));
                Trace($"  seek src=0x{start:X8} len=0x{checked(asset.SeekTableCount * 4):X}");
            });

        context.ResolvePointerInBlock(
            asset.PhysicalDataPtr,
            XFILE_BLOCK.PHYSICAL,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> pointer) =>
            {
                var start = pointerContext.Position;
                pointer.SetResult(pointerContext.ReadPointerValue(
                    pointer,
                    (ref XFileReadContext valueContext) =>
                        valueContext.ReadBytes(Math.Max(0, asset.PhysicalDataByteCount))));
                Trace($"  physical src=0x{start:X8} len=0x{Math.Max(0, asset.PhysicalDataByteCount):X}");
            });

        return asset;
    }

    public static ZonePointer<LoadedSound> ReadLoadedSoundPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<LoadedSound>("LoadedSoundAssetRef");
        context.ResolvePointerInBlock(pointer, XFILE_BLOCK.TEMP, ReadLoadedSoundPointerValue);
        return pointer;
    }

    public static void ReadLoadedSoundPointerValue(
        ref XFileReadContext context,
        ZonePointer<LoadedSound> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }

    private static void Trace(string message)
    {
        if (TraceLoadedSound)
            Console.Error.WriteLine($"[loaded-sound-trace] {message}");
    }
}
