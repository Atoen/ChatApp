using System.Collections.Concurrent;
using HttpServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HttpServer.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private static readonly ConcurrentDictionary<string, byte> Users = new();

    public async Task SendMessage(string message)
    {
        var user = Context.User?.Identity?.Name!;

        var message2 = new Message2
        {
            Author = user,
            Timestamp = DateTimeOffset.Now,
            Content = message
        };

        await Clients.All.ReceiveMessage(message2);
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name!;

        Users.TryAdd(user, default);

        var usernames = Users.Select(x => x.Key);
        await Clients.Caller.GetConnectedUsers(usernames);
        await Clients.Others.UserConnected(user);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Context.User?.Identity?.Name!;

        Users.TryRemove(user, out _);
        
        await Clients.All.UserDisconnected(user);
        await base.OnDisconnectedAsync(exception);
    }
}