using FastFile.Logic.Database.Streaming;

namespace FastFile.Logic.Database.DbFileLoad;

public readonly record struct DbHeader(
    string Magic,
    uint Version,
    bool AllowOnlineUpdate,
    ulong FileCreationTime,
    uint LanguageMask,
    uint SelectedLanguageMask,
    uint LanguageCount,
    uint SelectedLanguageIndex,
    uint EntryCount,
    DbHeaderImageStreamEntry[] ImageStreamEntries,
    uint FileSize,
    uint MaxFileSize,
    int PackedStreamOffset);
