using Server.Messages;
using Server.Users;

namespace Server.Commands;

public readonly struct CommandContext
{
    public CommandContext(TcpUser user, params string[] args)
    {
        User = user;
        Args = args;
    }

    public TcpUser User { get; }
    public string[] Args { get; }

    public async Task RespondAsync(string message, bool broadcast = false)
    {
        await User.WriteMessageAsync(Message.ServerResponse(message));
    }

    public async Task NotifyAsync(string notification)
    {
        await User.WriteMessageAsync(Message.ServerNotification(notification));
    }
}