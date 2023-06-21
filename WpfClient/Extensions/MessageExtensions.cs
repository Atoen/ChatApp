using System;
using System.Threading.Tasks;
using DSaladin.FontAwesome.WPF;
using RestSharp;
using WpfClient.Models;
using WpfClient.Views.UserControls;

namespace WpfClient.Extensions;

public static class MessageExtensions
{
    [STAThread]
    public static async Task ParseEmbedDataAsync(this Message message, IRestClient client)
    {
        if (message.Embed is {Type: EmbedType.Image})
        {
            message.UiEmbed = new ImageEmbed
            {
                ImageSource = message.Embed["Uri"]
            };
        }

        else if (message.Embed is not null)
        {
            message.UiEmbed = new FileEmbed
            {
                FileType = FileType.Default,
                DownloadUrl = message.Embed["Uri"],
                FileName = message.Embed["Filename"],
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
}