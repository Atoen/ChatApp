using System.Text.Json.Serialization;
using Server.Commands;
using Server.Users;

namespace Server.Messages;

public class Message
{
    public User Author { get; }
    public string Content { get; }
    public DateTimeOffset TimeStamp { get; }
    public MessageType Type { get; }

    public static User SystemSender => CommandHandler.CommandResponder;

    public Message(User author, string content, MessageType type = MessageType.Default)
    {
        Author = author;
        Content = content;
        Type = type;

        TimeStamp = DateTimeOffset.Now;
    }

    [JsonConstructor]
    private Message(User author, string content, MessageType type, DateTimeOffset timeStamp)
    {
        Author = author;
        Content = content;
        Type = type;
        TimeStamp = timeStamp;
    }

    public static Message ServerResponse(string response)
    {
        return new Message(SystemSender, response, MessageType.Private);
    }

    public static Message ServerNotification(string notification)
    {
        return new Message(SystemSender, notification, MessageType.SystemNotification);
    }

    public static Message ServerBroadcast(string broadcast)
    {
        return new Message(SystemSender, broadcast);
    }

    public override string ToString()
    {
        return Type switch
        {
            MessageType.SystemNotification => Content,
            _ => $"[{Author.Username}] {Content}"
        };
    }
}

public enum MessageType
{
    Default,
    Private,
    SystemNotification
}