using FastFile.Models.Assets.Localize;
using FastFile.Models.Codecs;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.Localize;

public sealed class LocalizeEmitter : IXAssetEmitter<LocalizeAsset>
{
    public IXAssetCodecContract Contract => LocalizeCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, LocalizeAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        string value = asset.Value ?? throw new InvalidDataException("Localize value is required for inline PS3 emission.");
        string name = asset.Name ?? throw new InvalidDataException("Localize name is required for inline PS3 emission.");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress rootAddress = context.Blocks.AllocateCurrent(LocalizeAsset.SerializedSize);

            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(-1);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitInlineXString(context, rootAddress.Add(0x00), value);
                EmitInlineXString(context, rootAddress.Add(0x04), name);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"Localize emitted source=0x{sourceOffset:X} value='{value}' name='{name}' blocks={context.Blocks.DescribePositions()}");
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static void EmitInlineXString(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        string value)
    {
        context.Blocks.PatchInlinePointerCell(pointerCellAddress);
        context.Blocks.AllocateCurrent(checked(System.Text.Encoding.Latin1.GetByteCount(value) + 1));
        context.Source.WriteCString(value);
    }
}
