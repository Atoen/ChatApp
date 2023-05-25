using System.Text.Json;
using Server.Attributes;
using Server.Commands;
using Server.Net;

namespace Server.Modules;

public class WebModule : Module
{
    private readonly HttpClient _httpClient;
    private readonly FileTransferManager _fileTransferManager;

    public WebModule(HttpClient httpClient, FileTransferManager fileTransferManager)
    {
        _httpClient = httpClient;
        _fileTransferManager = fileTransferManager;
    }

    [Command("reddit"), Alias("r")]
    public async Task HttpCommand(CommandContext context,
        [Summary("Subreddit to search")] string subreddit,
        [Summary("Number of posts to show")] int number = 5,
        [Summary("Time period")] string time = "week")
    {
        var response = await _httpClient.GetAsync($"https://www.reddit.com/r/{subreddit}/top.json?t={time}&limit={number}").ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }

        var responseContent = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var jsonObject = await JsonDocument.ParseAsync(responseContent).ConfigureAwait(false);

        var posts = jsonObject.RootElement.GetProperty("data").GetProperty("children");

        foreach (var post in posts.EnumerateArray())
        {
            var postTitle = post.GetProperty("data").GetProperty("title").GetString();
            await context.RespondAsync(postTitle!).ConfigureAwait(false);
        }
    }

    [Command("download")]
    public async Task DownloadCommand(CommandContext context)
    {
        if (!_fileTransferManager.IsFileAvailable)
        {
            await context.RespondAsync("No files available to download.");
        }
    }
}