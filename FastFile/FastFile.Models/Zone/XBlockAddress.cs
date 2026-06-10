using FastFile.Models.Zone;

namespace FastFile.Models.Zone;

public readonly record struct XBlockAddress(XFILE_BLOCK Block, int Offset);