using System.Text;
using Blazor.Server.Models;
using Blazor.Shared;
using tusdotnet.Interfaces;

namespace Blazor.Server.Services;

public class EmbedService
{
    private readonly ImagePreviewGeneratorService _previewGeneratorService;
    private static readonly string[] ImageExtensions = {".jpg", ".jpeg", ".png"};

    public EmbedService(ImagePreviewGeneratorService previewGeneratorService)
    {
        _previewGeneratorService = previewGeneratorService;
    }

    public async Task<Embed> CreateFileEmbed(ITusFile file, HttpContext context)
    {
        var metadata = await file.GetMetadataAsync(context.RequestAborted);

        var filename = metadata["filename"].GetString(Encoding.UTF8);
        var length = metadata["filesize"].GetString(Encoding.UTF8);
        var uri = CreateUri(file.Id, context);

        if (!HasImageExtension(filename))
        {
            return new Embed
            {
                Type = EmbedType.File,
                Data = new Dictionary<string, string>
                {
                    { "Uri", uri },
                    { "Filename", filename },
                    { "FileSize", length }
                }
            };
        }

        var width = metadata["width"].GetString(Encoding.UTF8);
        var height = metadata["height"].GetString(Encoding.UTF8);

        var preview = uri;

        var (widthInt, heightInt) = (int.Parse(width), int.Parse(height));
        if (widthInt > ImagePreviewGeneratorService.MaxWidth || heightInt > ImagePreviewGeneratorService.MaxHeight)
        {
            var previewId = await _previewGeneratorService.CreateImagePreviewAsync(file, context.RequestAborted);
            preview = CreateUri(previewId, context);
        }

        return new Embed
        {
            Type = EmbedType.Image,
            Data = new Dictionary<string, string>
            {
                { "Preview", preview },
                { "Uri", uri },
                { "Width", width },
                { "Height", height }
            }
        };
    }

    private bool HasImageExtension(string filename) => ImageExtensions.Any(filename.EndsWith);

    private string CreateUri(string id, HttpContext context)
    {
        return $"{context.Request.Scheme}://{context.Request.Host}/api/File?id={id}";
    }
}