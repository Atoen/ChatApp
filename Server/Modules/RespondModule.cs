using Server.Attributes;

namespace Server.Modules;

[Name("Basic Commands")]
public class RespondModule : Module
{
    [Command("ping"), Alias("p"), Summary("Pings the pong.")]
    public async Task Ping()
    {
        await Task.CompletedTask;
    }
    
    [Command("ping")]
    public async Task PingCommand(User user)
    {
        // await _server.Respond(user, "Pong!");
    }
    
    [Command("me")]
    public async Task MeCommand(User user, [Remainder] string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            // await _server.BroadcastMessage(SystemMessage.SystemMessageSender, text);
        }
    }
    
    [Command("time")]
    public async Task TimeCommand(User user)
    {
        // await _server.Respond(user, DateTime.Now.ToString(CultureInfo.InvariantCulture));
    }
    
    [Command("help")]
    public async Task HelpCommand(User user)
    {
        // foreach (var command in Handlers.Keys)
        // {
        //     // await _server.Respond(user, command);
        // }
    }
    
    [Command("list")]
    public async Task ListCommand(User user)
    {
        // foreach (var connected in _server.ConnectedUsers)
        // {
        //     await _server.Respond(user, connected.Username);
        // }
    }
}