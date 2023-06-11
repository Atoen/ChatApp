namespace HttpServer.Models;

public class User
{
    public required string Username { get; set; }
    public required byte[] Salt { get; set; }
    public required string PasswordHash { get; set; }
    public required Guid Id { get; set; }
}

public class UserDto
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
