using FastFile.Logic;
using FastFile.Logic.Database;

namespace Scratch;

class Program
{
    const string root = "/Users/jacob/Repositories/FastFile/Data/official_ff";
    const string patch_mp = $"{root}/patch_mp.ff";
    const string common_mp = $"{root}/common_mp.ff";
    const string mp_boneyard_load = $"{root}/mp_boneyard_load.ff";
    
    static void Main(string[] args)
    {
        OpenFF(common_mp);
    }

    static void OpenFF(string path)
    {
        byte[] buffer = File.ReadAllBytes(path);
        int length = buffer.Length;
        
        var ffReader = new FastFileReader(buffer, length);
        var hdr = ffReader.ParseHeader();
        
        byte[] zone = ffReader.UnpackZone();
    }

    static void OpenZone(byte[] zone)
    {
        var reader = new XFileReader(zone);
        var hdr = reader.Header;
        var list = reader.XAssetList;

        int bp = 0;
    }
}