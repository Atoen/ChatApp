using System.Text.Json;
using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

public class WebModule : Module
{
    private readonly HttpClient _httpClient;

    public WebModule(HttpClient httpClient) => _httpClient = httpClient;

    [Command("reddit"), Alias("r")]
    public async Task HttpCommand(CommandContext context, [Remainder] string subreddit = "all")
    {
        var response = await _httpClient.GetAsync($"https://www.reddit.com/r/{subreddit}/top.json?t=week&limit=5");
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(response.ReasonPhrase);
        }

        var responseContent = await response.Content.ReadAsStreamAsync();
        var jsonObject = await JsonDocument.ParseAsync(responseContent);

        var posts = jsonObject.RootElement.GetProperty("data").GetProperty("children");

        foreach (var post in posts.EnumerateArray())
        {
            var postTitle = post.GetProperty("data").GetProperty("title").GetString();
            await context.RespondAsync(postTitle!);
        }
    }
}