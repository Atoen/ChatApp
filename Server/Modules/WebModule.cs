using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

public class WebModule : Module
{
    private readonly HttpClient _httpClient;

    public WebModule(HttpClient httpClient) => _httpClient = httpClient;

    [Command("http")]
    public async Task HttpCommand(CommandContext context, [Remainder] string address)
    {
        var response = await _httpClient.GetAsync(address);

        await context.Respond(response.ToString());
    }
}