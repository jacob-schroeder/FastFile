using FastFile.Models.Database.Streaming;

namespace FastFile.Models.Database.DbFileLoad;

public readonly record struct DbHeader(
    string Magic,
    XFileVersion Version,
    bool AllowOnlineUpdate,
    DateTime FileCreationTime,
    uint LanguageMask,
    uint SelectedLanguageMask,
    uint LanguageCount,
    uint SelectedLanguageIndex,
    uint EntryCount,
    DbHeaderImageStreamEntry[] ImageStreamEntries,
    uint FileSize,
    uint MaxFileSize,
    int PackedStreamOffset);
