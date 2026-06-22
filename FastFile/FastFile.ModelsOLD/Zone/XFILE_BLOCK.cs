namespace FastFile.ModelsOLD.Zone;

public enum XFILE_BLOCK : int
{
  TEMP	= 0,
  PHYSICAL	= 1,
  RUNTIME	= 2,
  VIRTUAL	= 3,
  LARGE	= 4,
  CALLBACK	= 5,
  VERTEX	= 6,
  XFILE_BLOCK_VERTEX = VERTEX,
  MAX_XFILE_COUNT
}
