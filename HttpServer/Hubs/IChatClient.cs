
using HttpServer.Models;

namespace HttpServer.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(HubMessage message);

    Task UserConnected(string user);

    Task UserDisconnected(string user);

    Task GetConnectedUsers(IEnumerable<string> users);
}