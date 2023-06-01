namespace HttpServer.Models;

public class Message
{
    public User Author { get; set; } = null!;
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = null!;
}

public class Message2
{
    public string Author { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = null!;
}