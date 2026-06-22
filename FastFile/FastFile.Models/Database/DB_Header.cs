namespace FastFile.Models.Database;

public class DB_Header
{
    public string Magic { get; set; } //8 bytes.
    
    public XFileVersion Version { get; set; }

    public bool AllowOnlineUpdate { get; set; }
    
    public UInt64 FileCreationTime { get; set; }

    public Language Region { get; set; }

    public int EntryCount { get; set; }
    
    public ImageStreamEntry[] ImageStreamEntries { get; set; } = [];
    
    public int FileSize { get; set; }
    
    public int MaxFileSize { get; set; }
}