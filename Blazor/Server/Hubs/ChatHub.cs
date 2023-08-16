using System.Collections.Concurrent;
using Blazor.Server.Models;
using Blazor.Server.Services;
using Blazor.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Blazor.Server.Hubs;

[Authorize]
public class ChatHub : Hub<IChatClient>
{
    private readonly UserService _userService;
    private readonly MessageService _messageService;
    private readonly GifSourceVerifierService _gifSourceVerifierService;
    private static readonly ConcurrentDictionary<string, byte> Users = new();

    public ChatHub(UserService userService, MessageService messageService, GifSourceVerifierService gifSourceVerifierService)
    {
        _userService = userService;
        _messageService = messageService;
        _gifSourceVerifierService = gifSourceVerifierService;
    }
    
    public async Task SendMessage(string messageContent)
    {
        var user = await _userService.GetUser(Context.User!);

        var isGifSource = await _gifSourceVerifierService.VerifyAsync(messageContent);

        var message = new Message
        {
            Author = user.AsT0,
            Timestamp = DateTimeOffset.Now,
            Content = isGifSource ? string.Empty : messageContent
        };

        if (isGifSource)
        {
            message.Embed = new Embed
            {
                Type = EmbedType.Gif,
                Data = new()
                {
                    { "Uri", messageContent }
                }
            };
        }
        
        await _messageService.StoreMessage(message);
        await Clients.All.ReceiveMessage(message);
    }
    
    public override async Task OnConnectedAsync()
    {
        var user = Context.User?.Identity?.Name!;

        Users.TryAdd(user, default);

        var usernames = Users.Select(x => x.Key);

        await Clients.Caller.GetConnectedUsers(usernames);
        await Clients.Others.UserConnected(user);
    }
    
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = Context.User?.Identity?.Name!;

        Users.TryRemove(user, out _);

        await Clients.All.UserDisconnected(user);
    }
}