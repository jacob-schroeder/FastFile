namespace FastFile.UI;

public static class FileHandler
{
    public static byte[]? FileContents = null;
    public static bool FileOpened => FileContents is { Length: > 0 };
}