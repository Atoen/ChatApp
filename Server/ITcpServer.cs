using Server.Messages;
using Server.Users;

namespace Server;

public interface ITcpServer
{
    Task BroadcastMessageAsync(Message message);

    Task DisconnectUserOnError(TcpUser user);
}