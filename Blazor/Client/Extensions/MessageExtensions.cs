using Blazor.Client.Models;
using Blazor.Shared;
using RestSharp;

namespace Blazor.Client.Extensions;

public static class MessageExtensions
{
    public static async Task<MessageModel> ToModelAsync(this MessageDto message, IRestClient client)
    {
        var model = new MessageModel
        {
            Author = message.Author.Username,
            Content = message.Content,
            Timestamp = message.Timestamp
        };

        if (message.Embed is { Type: EmbedType.Image })
        {
            model.Embed = new EmbedModel
            {
                Type = EmbedType.Image,
                Data =
                {
                    { "Source", message.Embed["Uri"] }
                }
            };
        }

        else if (message.Embed is { Type: EmbedType.File })
        {
            var filename = message.Embed["Filename"];
            var fileType = GetFileType(filename).ToString();

            model.Embed = new EmbedModel
            {
                Type = EmbedType.File,
                Data =
                {
                    { "Source", message.Embed["Uri"] },
                    { "Filename", filename },
                    { "FileSize", message.Embed["FileSize"] },
                    { "FileType", fileType }
                }
            };
        }

        else if ((message.Content.StartsWith("https://media.tenor.com/") ||
            message.Content.StartsWith("https://media.giphy.com/")) &&
            Uri.IsWellFormedUriString(message.Content, UriKind.Absolute))
        {
            if (await VerifyGifSource(message.Content, client))
            {
                model.Embed = new EmbedModel
                {
                    Type = EmbedType.Gif,
                    Data =
                    {
                        { "Source", message.Content }
                    }
                };
            }
        }

        return model;
    }

    private static async Task<bool> VerifyGifSource(string source, IRestClient client)
    {
        var request = new RestRequest(source, Method.Head);

        try
        {
            var response = await client.HeadAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private const string Pdf = "pdf";
    private const string Text = "txt";
    private static readonly List<string> Archive = new() { "zip", "rar", "7z" };
    private static readonly List<string> Code = new() { "cs", "java", "cpp", "py", "js", "c" };
    private static readonly List<string> Video = new() { "mp4", "avi", "mov", "mkv" };

    private static FileType GetFileType(string filename)
    {
        var tokens = filename.Split('.');

        if (tokens.Length < 2) return FileType.Default;
        var extension = tokens[^1].ToLower();

        if (extension == Pdf) return FileType.Pdf;
        if (extension == Text) return FileType.Text;

        if (Archive.Contains(extension)) return FileType.Archive;
        if (Code.Contains(extension)) return FileType.Code;
        if (Video.Contains(extension)) return FileType.Video;

        return FileType.Default;
    }
}

public enum FileType
{
    Default,
    Pdf,
    Text,
    Archive,
    Code,
    Video
}