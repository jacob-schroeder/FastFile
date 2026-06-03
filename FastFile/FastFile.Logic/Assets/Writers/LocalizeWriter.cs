using FastFile.Logic.Zone;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Localize;

namespace FastFile.Logic.Assets.Writers;

internal static class LocalizeWriter
{
    public static void Write(ZoneWriterContext context, BaseAsset asset)
    {
        var localize = (LocalizeEntry)asset;
        GenericWriter.WriteStringPointer(context, localize.ValuePtr);
        GenericWriter.WriteStringPointer(context, localize.NamePtr);
    }
}
