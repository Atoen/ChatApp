namespace HttpServer.Models;

public class Message
{
    public User? Author { get; set; }
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Content { get; set; }
}