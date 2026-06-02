namespace FastFile.Models.Zone;

public enum XFILE_BLOCK : int
{
  TEMP	= 0,
  PHYSICAL	= 1,
  RUNTIME	= 2,
  VIRTUAL	= 3,
  LARGE	= 4,
  CALLBACK	= 5,
  #if PC || PS3
   XFILE_BLOCK_VERTEX	= 6,
   #if PC
   XFILE_BLOCK_INDEX	= 7,
  #endif
  #endif
  MAX_XFILE_COUNT
}