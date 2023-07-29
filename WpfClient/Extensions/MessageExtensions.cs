using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;
using WpfClient.Models;
using WpfClient.Views.UserControls.Embeds;

namespace WpfClient.Extensions;

public static class MessageExtensions
{
    public static async Task ParseEmbedDataAsync(this Message message, IRestClient client)
    {
        if (message.Embed is {Type: EmbedType.Image})
        {
            message.UiEmbed = new ImageEmbed
            {
                ImageSource = message.Embed["Uri"]
            };
        }

        else if (message.Embed is {Type: EmbedType.File})
        {
            var filename = message.Embed["Filename"];

            message.UiEmbed = new FileEmbed
            {
                FileType = GetFileType(filename),
                DownloadUrl = message.Embed["Uri"],
                FileName = filename,
                FileSize = long.Parse(message.Embed["FileSize"])
            };
        }

        else if (message.Content.StartsWith("https://media.tenor.com/") || message.Content.StartsWith("https://media.giphy.com/")
                 && Uri.IsWellFormedUriString(message.Content, UriKind.Absolute))
        {
            if (!await VerifyGifSource(message.Content, client)) return;

            message.Embed = new Embed {Type = EmbedType.Gif};
            message.UiEmbed = new GifEmbed
            {
                GifSource = message.Content
            };
        }
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
    private static readonly List<string> Archive = new() {"zip", "rar", "7z"};
    private static readonly List<string> Code = new() {"cs", "java", "cpp", "py", "js", "c"};
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