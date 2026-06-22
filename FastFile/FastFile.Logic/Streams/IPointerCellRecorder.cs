using FastFile.Models;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Logic.Streams;

internal interface IPointerCellRecorder
{
    XBlockAddress CurrentWriteAddress { get; }
    void RecordPointerCell(XBlockAddress address, int raw);
}
