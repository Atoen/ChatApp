using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

[Name("Basic Commands")]
public class RespondModule : Module
{
    [Command("ping"), Alias("p"), Summary("Pongs the ping")]
    public async Task Ping() => await Context.Respond("Pong!");

    [Command("me")]
    public async Task MeCommand([Remainder] string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await Context.User.Respond(text);
        }
    }
    
    [Command("time"), Alias("t"), Summary("Displays current server time")]
    public async Task TimeCommand() => await Context.Respond(DateTime.Now.ToLongTimeString());
}
