using FastFile.Models.Data;

namespace FastFile.Logic.Zone;

public delegate void XFilePointerWriter<T>(
    XFileWriterContext context,
    ZonePointer<T> pointer);
