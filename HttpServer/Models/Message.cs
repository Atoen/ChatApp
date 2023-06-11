namespace HttpServer.Models;

public class Message
{
    public User Author { get; set; } = null!;
    public Guid Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = null!;
}

public class HubMessage
{
    public string Author { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string Content { get; set; } = null!;

    public Embed? Embed { get; set; }
}

public class Embed
{
    public EmbedType Type { get; set; }
    public Dictionary<string, string> EmbedData { get; set; } = default!;
}

public enum EmbedType
{
    File,
    Image,
    Gif
}