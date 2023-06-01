using Microsoft.AspNetCore.Identity;

namespace HttpServer.Models;

public class User : IdentityUser
{
    public string Username { get; set; } = null!;
    public byte[] Salt { get; set; } = null!;
}

public class UserDto
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
