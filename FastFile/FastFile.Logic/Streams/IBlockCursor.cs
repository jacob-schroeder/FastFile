namespace FastFile.Logic.Streams;

public interface IBlockCursor : IWriteCursor
{
    int DB_AllocStreamPos(int bytes); // advance cursor + return start
    void DB_IncStreamPos(int bytes);
    void PatchInt32(int offset, int value); // when alias/fixups are needed
    byte[] ToArray();
}
