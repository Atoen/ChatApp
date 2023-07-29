using Blazor.Shared;

namespace Blazor.Server.Models;

public class Message
{
    public int Id { get; set; }
    public User Author { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = string.Empty;
    public Embed? Embed { get; set; }

    public MessageDto ToDto()
    {
        return new MessageDto
        {
            Author = Author.ToDto(),
            Id = Id,
            Timestamp = Timestamp,
            Content = Content,
            Embed = Embed?.ToDto()
        };
    }
}

//public class MessageDto
//{
//    public required MessageAuthorDto Author { get; set; }
//    public int Id { get; set; }
//    public DateTimeOffset Timestamp { get; set; }
//    public required string Content { get; set; }
//    public Embed? Embed { get; set; }
//}


