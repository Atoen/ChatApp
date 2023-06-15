using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Stores;

namespace HttpServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> DownloadFile([FromQuery] string id)
    {
        var store = new TusDiskStore(@"D:\Tus\");

        ITusFile file;
        try
        {
            file = await store.GetFileAsync(id, HttpContext.RequestAborted);
        }
        catch (TusStoreException e)
        {
            return BadRequest(e.Message);
        }

        var metadata = await file.GetMetadataAsync(HttpContext.RequestAborted);

        var contentType = metadata.TryGetValue("contentType", out var typeMeta)
            ? typeMeta.GetString(Encoding.UTF8)
            : "application/octet-stream";

        if (metadata.TryGetValue("filename", out var nameMeta))
        {
            var name = nameMeta.GetString(Encoding.UTF8);
            var contentDisposition = new ContentDisposition
            {
                FileName = name,
                Inline = false
            };

            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
        }

        var fileStream = await file.GetContentAsync(HttpContext.RequestAborted);
        return File(fileStream, contentType);
    }
}