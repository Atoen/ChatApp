using Blazor.Shared;

namespace Blazor.Server.Models;

public class User
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required byte[] Salt { get; set; }
    public required string PasswordHash { get; set; }

    // public ICollection<Message> Messages { get; set; } = new List<Message>();

    public MessageAuthorDto ToDto()
    {
        return new MessageAuthorDto
        {
            Username = Username,
            Id = Id
        };
    }
}

//public class MessageAuthorDto
//{
//    public required string Username { get; set; }
//    public required Guid Id { get; set; }
//}
