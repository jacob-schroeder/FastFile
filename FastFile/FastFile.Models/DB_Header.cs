using System.ComponentModel.DataAnnotations;
using FastFile.Models.Archive;

namespace FastFile.Models;

public class DB_Header
{
    [Length(8, 8)]
    public string Magic { get; set; }
    
    public XFILE_VERSION Version { get; set; } //should be 0x10D

    public bool AllowOnlineUpdate { get; set; }
    
    public UInt64 FileCreationTime { get; set; }

    public Language Region { get; set; }

    public int EntryCount { get; set; }
    
    //public Entry_t[] Assets { get; set; }
    
    public int FileSize { get; set; }
    
    public int MaxFileSize { get; set; }
}