using System.Net.Mime;
using HttpServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HttpServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;

    public FileController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<string>> UploadFileAsync(IFormFile file)
    {
        if (file is not {Length: > 0})
        {
            return BadRequest();
        }

        var uniqueFilename = await _fileService.SaveFile(file);
        var fileUri = Url.Action(nameof(DownloadFile), nameof(File), new {id = uniqueFilename}, Request.Scheme);

        return Ok(fileUri);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadFile([FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return BadRequest("Invalid file id.");
        }

        var result = await _fileService.GetFile(id);
        var response =  result.Match<IActionResult>(
            tuple =>
            {
                var (name, content) = tuple;
                var contentDisposition = new ContentDisposition
                {
                    FileName = name,
                    Inline = false
                };

                Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
                return File(content, "application/octet-stream");
            },
            notFound => NotFound());

        return response;
    }
}