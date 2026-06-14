using FastFile.Logic.Zone;
using FastFile.Models.Zone;

namespace FastFile.Tests;

public sealed class XBlockTests
{
    [Fact]
    public void ZeroSizedBlockCanGrowWhenLoadersAllocateIntoIt()
    {
        var block = new XBlock(XFILE_BLOCK.RUNTIME, 0);

        block.WriteInt32(unchecked((int)0x12345678));

        Assert.Equal(4, block.WrittenSpan.Length);
        Assert.Equal(4, block.BlockSpan.Length);
        Assert.Equal(unchecked((int)0x12345678), block.ReadInt32(0));
    }
}
