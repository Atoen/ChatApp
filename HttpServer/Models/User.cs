namespace HttpServer.Models;

public class User
{
    public required string Username { get; set; } = null!;
    public required byte[] Salt { get; set; } = null!;
    public required string PasswordHash { get; set; } = null!;
    public required Guid Id { get; set; }
}

public class UserDto
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
