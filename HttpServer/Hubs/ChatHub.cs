using Microsoft.AspNetCore.SignalR;

namespace HttpServer.Hubs;

public class ChatHub : Hub
{
    private readonly IUserManager _userManager; // Custom user manager to retrieve usernames

    public ChatHub(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public override async Task OnConnectedAsync()
    {
        var username = await _userManager.GetUsernameByContextAsync(Context);

        if (!string.IsNullOrEmpty(username))
        {
            // Notify clients that a user has joined with the username
            await Clients.All.SendAsync("UserJoined", username);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var username = await _userManager.GetUsernameByContextAsync(Context) ?? "Małe oro";

        if (!string.IsNullOrEmpty(username))
        {
            // Notify clients that a user has left with the username
            await Clients.All.SendAsync("UserLeft", username);
        }

        await base.OnDisconnectedAsync(exception);
    }
}