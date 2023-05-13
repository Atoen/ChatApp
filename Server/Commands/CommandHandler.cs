using Serilog;
using Server.Commands.Attributes;
using Server.Packets;

namespace Server.Commands;

public class CommandHandler : ICommandHandler
{
    public CommandHandler(TcpServer server, char prefix, bool caseSensitive = false)
    {
        _server = server;
        
        Prefix = prefix;
        CaseSensitive = caseSensitive;
        
        SetUp();
    }
    
    [InitializeHandler] private void SetUp() {}

    public char Prefix { get; set; }
    public bool CaseSensitive { get; set; }
    public Dictionary<string, CommandExecutionContext> Handlers { get; } = new();

    private readonly TcpServer _server;

    public async Task Handle(User sender, string command)
    {
        var commandBody = command[1..];
        var args = commandBody.Split(' ');
        
        if (args.Length < 1) return;
        if (!CaseSensitive) args[0] = args[0].ToLower();

        if (!Handlers.TryGetValue(args[0], out var handler)) return;

        try
        {
            Log.Debug("Executing '{Command}' for {User}", command, sender.Username);
            await handler.Invoke(sender, args[1..]);
        }
        catch (Exception e)
        {
            Log.Information("Error while executing {Command} for {User}: {Error}",
                command, sender.Username, e.Message);

            await _server.Respond(sender, e.Message);
        }
    }

    [Command("Ping")]
    private async Task PingCommand(User user)
    {
        await _server.Respond(user, "Pong!");
    }

    [Command("Me")]
    private async Task MeCommand(User user, [Remainder] string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await _server.BroadcastMessage(SystemMessage.SystemMessageSender, text);
        }
    }
}