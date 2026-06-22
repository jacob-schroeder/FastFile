using FastFile.ModelsOLD.Zone;

namespace UI.Models;

public sealed class BlockStreamDisplayItem
{
    public required XFILE_BLOCK Block { get; init; }

    public required int Index { get; init; }

    public required string Name { get; init; }

    public required string SizeText { get; init; }

    public required string DeclaredSizeText { get; init; }

    public required byte[] Data { get; init; }
}
