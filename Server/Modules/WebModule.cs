using System.Text.Json;
using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

public class WebModule : Module
{
    private readonly HttpClient _httpClient;

    public WebModule(HttpClient httpClient) => _httpClient = httpClient;

    [Command("reddit"), Alias("r")]
    public async Task HttpCommand(CommandContext context, string subreddit = "all")
    {
        var response = await _httpClient.GetAsync($"https://www.reddit.com/r/{subreddit}/top.json?t=week&limit=5").ConfigureAwait(false);
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
    
    [Command("reddit"), Alias("r")]
    public async Task HttpCommand(CommandContext context, string subreddit, int number)
    {
        var response = await _httpClient.GetAsync($"https://www.reddit.com/r/{subreddit}/top.json?t=week&limit={number}").ConfigureAwait(false);
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
    
    [Command("reddit"), Alias("r")]
    public async Task HttpCommand(CommandContext context, string subreddit, int number, string time)
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
}