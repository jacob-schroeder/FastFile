using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Eboot;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class ImpactFxReader
{
    private const int EntryCount = 15;

    public static FxImpactTable Read(ref XFileReadContext context)
    {
        var asset = new FxImpactTable
        {
            Offset = context.Position,
            NamePtr = context.ReadDirectPointer<string>("FxImpactTable.Name"),
            Table = context.ReadDirectPointer<FxImpactEntry[]>("FxImpactTable.Table"),
        };

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ResolveImpactEntries(ref context, asset.Table);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    private static void ResolveImpactEntries(
        ref XFileReadContext context,
        ZonePointer<FxImpactEntry[]> pointer)
    {
        context.ResolveInlinePointer(pointer, (ref XFileReadContext pointerContext, ZonePointer<FxImpactEntry[]> p) =>
        {
            p.SetResult(pointerContext.ReadPointerValue(
                p,
                (ref XFileReadContext valueContext) =>
                {
                    var entries = new FxImpactEntry[EntryCount];
                    for (var i = 0; i < entries.Length; i++)
                        entries[i] = ReadImpactEntry(ref valueContext);

                    return entries;
                }));
        });
    }

    private static FxImpactEntry ReadImpactEntry(ref XFileReadContext context)
    {
        var entry = new FxImpactEntry();

        for (var i = 0; i < entry.NonFlesh.Length; i++)
            entry.NonFlesh[i] = FxReader.ReadFxPointer(ref context);

        for (var i = 0; i < entry.Flesh.Length; i++)
            entry.Flesh[i] = FxReader.ReadFxPointer(ref context);

        return entry;
    }
}
