using FastFile.Models.Assets.RawFile;
using FastFile.Models.Codecs;
using FastFile.Models.Zone;

namespace FastFile.Emitters.Assets.RawFile;

public sealed class RawFileEmitter : IXAssetEmitter<RawFileAsset>
{
    public IXAssetCodecContract Contract => RawFileCodecContracts.Asset;

    public void EmitAsset(XEmitContext context, RawFileAsset asset)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(asset);

        string name = asset.Name ?? throw new InvalidDataException("RawFile name is required for inline PS3 emission.");
        byte[] buffer = asset.Buffer ?? throw new InvalidDataException("RawFile buffer is required for inline PS3 emission.");
        int bufferLength = asset.BufferLength;
        if (buffer.Length != bufferLength)
            throw new InvalidDataException($"RawFile buffer length is {buffer.Length}, but loader contract requires {bufferLength} byte(s).");

        int sourceOffset = context.Source.Offset;
        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            XBlockAddress rootAddress = context.Blocks.AllocateCurrent(RawFileAsset.SerializedSize);

            context.Source.WriteInt32(-1);
            context.Source.WriteInt32(asset.CompressedLen);
            context.Source.WriteInt32(asset.Len);
            context.Source.WriteInt32(-1);

            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                EmitInlineXString(context, rootAddress.Add(0x00), name);
                EmitInlineBytes(context, rootAddress.Add(0x0C), buffer);
            }
            finally
            {
                context.Blocks.Pop();
            }

            context.Diagnostics.Trace(
                $"RawFile emitted source=0x{sourceOffset:X} name='{name}' compressedLen={asset.CompressedLen} len={asset.Len} " +
                $"bufferLength={bufferLength} blocks={context.Blocks.DescribePositions()}");
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

    private static void EmitInlineBytes(
        XEmitContext context,
        XBlockAddress pointerCellAddress,
        ReadOnlySpan<byte> bytes)
    {
        context.Blocks.PatchInlinePointerCell(pointerCellAddress);
        context.Blocks.AllocateCurrent(bytes.Length);
        context.Source.WriteBytes(bytes);
    }
}
