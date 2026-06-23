using System.Globalization;
using FastFile.Loaders;
using FastFile.Loaders.DbFileLoad;
using FastFile.Models.Database;
using FastFile.Models.Database.DbFileLoad;
using FastFile.Models.Database.Streaming;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace Scratch;

class Program
{
    const string scratchRoot = "/Users/jacob/Repositories/FastFile/FastFile/Scratch";
    const string root = "/Users/jacob/Repositories/FastFile/Data/official_ff";
    const string patch_mp = $"{root}/patch_mp.ff";
    const string common_mp = $"{root}/common_mp.ff";
    const string mp_boneyard_load = $"{root}/mp_boneyard_load.ff";

    static void Main(string[] args)
    {
        string path = common_mp;
        
        byte[] buffer = File.ReadAllBytes(path);
        int len = buffer.Length;

        var loader = new FastFileLoader();
        var session = loader.Load(buffer, len);

        var ffheader = session.Header;
        var xheader = session.XFileHeader;
        
        Console.WriteLine($"Magic: {ffheader.Magic}");
        Console.WriteLine($"Version: {ffheader.Version}");
        Console.WriteLine($"Allow update: {ffheader.AllowOnlineUpdate}");
        Console.WriteLine($"Created: {ffheader.FileCreationTime:MM/dd/yyyy HH:mm:ss tt}");
        Console.WriteLine($"Language: {(Language)ffheader.LanguageMask}");
        Console.WriteLine($"Entry Count: {ffheader.EntryCount}");
        Console.WriteLine($"File Size: {ffheader.FileSize}");
        Console.WriteLine($"Max File Size: {ffheader.MaxFileSize}");
        Console.WriteLine("=====================");
        
        Console.WriteLine($"XFile Size: {xheader.Size}");
        Console.WriteLine($"XFile Ext Size: {xheader.ExternalSize}");
        for(int i = 0; i < xheader.BlockSize.Length; i++)
        {
            int size = xheader.BlockSize[i];
            Console.WriteLine($"{(XFileBlockType)i} Block Size: {size}");
        }
    }
}