using FastFile.LogicOLD.Zone;
using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;

namespace FastFile.TestsOLD;

public sealed class XBlockStreamPointerTests
{
    [Fact]
    public void AliasOffsetPointersDereferenceAliasedPointerCell()
    {
        var stream = CreateStream();
        var temp = stream.GetBlock(XFILE_BLOCK.TEMP);
        temp.WriteInt32(XPointer.EncodeOffset((int)XFILE_BLOCK.TEMP, 0x20));
        temp.WriteInt32(0);

        var ptr = new XPointer<byte[]>
        {
            Raw = XPointer.EncodeOffset((int)XFILE_BLOCK.TEMP, 0x00),
            Kind = PointerKind.Offset,
            ResolutionKind = PointerResolutionKind.Alias,
            PatchAddress = new XBlockAddress(XFILE_BLOCK.TEMP, 0x04)
        };

        var result = stream.MaterializePointer(
            ptr,
            XPointerMaterializationPlan.AtBlockPosition(
                XPointerTarget.ByteArray,
                PointerResolutionKind.Alias,
                XFILE_BLOCK.TEMP,
                offsetIsAliasCell: true));

        Assert.False(result.ShouldReadPayload);
        Assert.Equal(new XBlockAddress(XFILE_BLOCK.TEMP, 0x20), ptr.Address);
        Assert.Equal(
            XPointer.EncodeOffset((int)XFILE_BLOCK.TEMP, 0x20),
            stream.ReadInt32(new XBlockAddress(XFILE_BLOCK.TEMP, 0x04)));
    }

    [Fact]
    public void CurrentStreamBytePayloadsHonorAlignment()
    {
        var stream = CreateStream();
        stream.Write([0xAA, 0xBB]);

        var ptr = new XPointer<byte[]>
        {
            Raw = -1,
            Kind = PointerKind.Inline,
            ResolutionKind = PointerResolutionKind.Alias,
            PatchAddress = null
        };

        var result = stream.MaterializePointer(
            ptr,
            XPointerMaterializationPlan.CurrentStream(
                XPointerTarget.ByteArray,
                PointerResolutionKind.Alias,
                alignment: 4,
                offsetIsAliasCell: true));

        Assert.True(result.ShouldReadPayload);
        Assert.Equal(new XBlockAddress(XFILE_BLOCK.TEMP, 0x04), ptr.Address);
        Assert.Equal(0x04, stream.ActivePosition);
    }

    private static XBlockStream CreateStream()
    {
        return new XBlockStream(Enumerable.Repeat(0x100, Enum.GetValues<XFILE_BLOCK>().Length).ToArray());
    }
}
