using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.Localize;

namespace FastFile.Logic.Assets.Readers;

internal static class LocalizeReader
{
    public static LocalizeEntry Read(ref ZoneReadContext context)
    {
        return new LocalizeEntry
        {
            Offset = context.Position,
            ValuePtr = GenericReader.ReadStringPointer(ref context),
            NamePtr = GenericReader.ReadStringPointer(ref context),
        };
    }
}
