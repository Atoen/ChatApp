namespace HttpServer.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public byte[] Salt { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}

public class UserDto
{
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
}
