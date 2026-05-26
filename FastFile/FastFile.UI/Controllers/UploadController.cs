using FastFile.Logic;
using Microsoft.AspNetCore.Mvc;

namespace FastFile.UI.Controllers;

public class UploadController : Controller
{
    public async Task<IActionResult> FastFile(IFormFile? file)
    {
        if (file == null)
            return BadRequest("File is null");

        if (file.Length > int.MaxValue)
            return BadRequest("File is too large");

        var buffer = new byte[file.Length];

        await using Stream stream = file.OpenReadStream();

        int read = 0;
        while (read < buffer.Length)
        {
            int n = await stream.ReadAsync(buffer.AsMemory(read));
            if (n == 0) break;

            read += n;
        }

        var ffReader = new FastFileReader(buffer, buffer.Length);
        var header = ffReader.ParseHeader();
        var zone = ffReader.UnpackZone();

        var zoneReader = new ZoneReader(zone);
        var zoneHead =  zoneReader.ParseHeader();
        var assetList = zoneReader.ParseXAssetList();


        return RedirectToAction("Index", "Home");
        //return new EmptyResult();
    }
}