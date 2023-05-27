using Microsoft.AspNetCore.SignalR;

namespace HttpServer.Hubs;

public interface IUserManager
{
    Task<string?> GetUsernameByContextAsync(HubCallerContext context);
}

public class UserManager : IUserManager
{
    // Implement the methods in IUserManager based on your user authentication mechanism
    public Task<string?> GetUsernameByContextAsync(HubCallerContext context)
    {
        // Retrieve the username associated with the connection from your user authentication system
        // Example: return the authenticated user's username
        var user = context.User;
        var username = user?.Identity?.Name;
        return Task.FromResult(username);
    }
}