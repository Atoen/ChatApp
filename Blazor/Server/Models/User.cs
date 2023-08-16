using Blazor.Shared;

namespace Blazor.Server.Models;

public class User
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required byte[] Salt { get; set; }
    public required string PasswordHash { get; set; }
    public required List<RefreshToken> RefreshTokens { get; set; }

    public MessageAuthorDto ToDto()
    {
        return new MessageAuthorDto
        {
            Username = Username,
            Id = Id
        };
    }
}
