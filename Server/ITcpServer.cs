using Server.Messages;

namespace Server;

public interface ITcpServer
{
    Task BroadcastMessageAsync(Message message);
}