namespace HttpServer.Models;

public class Message
{
    public User? Author { get; set; }
    public DateTimeOffset Id { get; set; }
    public string? Content { get; set; }
}