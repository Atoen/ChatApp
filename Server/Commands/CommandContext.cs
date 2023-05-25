using Server.Messages;
using Server.Users;

namespace Server.Commands;

public readonly struct CommandContext
{
    public CommandContext(TcpUser user, params object[] args)
    {
        User = user;
        Args = args;
    }

    public TcpUser User { get; }
    public object[] Args { get; }

    public async Task RespondAsync(string message)
    {
        await User.WriteMessageAsync(Message.ServerResponse(message)).ConfigureAwait(false);
    }

    public async Task NotifyAsync(string notification)
    {
        await User.WriteMessageAsync(Message.ServerNotification(notification)).ConfigureAwait(false);
    }

    public async Task BroadcastAsync(string broadcast)
    {
        await User.Server.BroadcastMessageAsync(Message.ServerBroadcast(broadcast)).ConfigureAwait(false);
    }
}