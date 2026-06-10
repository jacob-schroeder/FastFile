using FastFile.Logic.Archive;
using FastFile.Logic.Zone;
using FastFile.Models.Archive;

namespace FastFile.Tests;

internal static class GoldenFastFileFixture
{
    public static GoldenFastFile ReadPatchMp(Action<int, int>? progress = null)
    {
        var path = FindRepositoryFile(Path.Combine("Data", "official_ff", "patch_mp.ff"));
        var buffer = File.ReadAllBytes(path);

        var fastFileReader = new FastFileReader(buffer, buffer.Length);
        var fastFileHeader = fastFileReader.ParseHeader();
        var zone = fastFileReader.UnpackZone();
        var zoneReader = new XFileReader(zone, progress).Read();

        return new GoldenFastFile(path, buffer, zone, fastFileHeader, zoneReader);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}

internal sealed record GoldenFastFile(
    string Path,
    byte[] Buffer,
    byte[] Zone,
    DB_Header FastFileHeader,
    XFileReader ZoneReader);
