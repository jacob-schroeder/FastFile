using FastFile.Models.Zone;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.XFileLoad;

public sealed class XFileHeaderReader
{
    public XFile Read(FastFileCursor cursor)
    {
        var header = new XFile
        {
            Size = cursor.ReadInt32(),
            ExternalSize = cursor.ReadInt32(),
            BlockSize = new int[(int)XFileBlockType.COUNT]
        };

        for (int i = 0; i < header.BlockSize.Length; i++)
            header.BlockSize[i] = cursor.ReadInt32();

        return header;
    }
}
