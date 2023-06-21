using System.Text;
using HttpServer.Models;
using tusdotnet.Interfaces;

namespace HttpServer.Services;

public class EmbedService
{
    private static readonly string[] ImageExtensions = {".jpg", ".jpeg", ".png"};

    public async Task<Embed> CreateEmbed(ITusFile file, HttpContext context)
    {
        var metadata = await file.GetMetadataAsync(context.RequestAborted);

        var filename = metadata["filename"].GetString(Encoding.UTF8);
        var length = metadata["length"].GetString(Encoding.UTF8);
        var uri = CreateUri(file, context);

        if (HasImageExtension(filename))
        {
            return new Embed
            {
                Type = EmbedType.Image,
                Data = new()
                {
                    {"Uri", uri}
                }
            };
        }

        return new Embed
        {
            Type = EmbedType.File,
            Data = new()
            {
                {"Uri", uri},
                {"Filename", filename},
                {"FileSize", length}
            }
        };
    }

    private bool HasImageExtension(string filename) => ImageExtensions.Any(filename.EndsWith);

    private string CreateUri(ITusFile file, HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}/api/File?id={file.Id}";
    }
}