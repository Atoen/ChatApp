namespace Server;

public class SystemMessage : Message
{
    public SystemMessage(string content) : base(content) { }

    public static string SystemMessageSender { get; set; } = "Server";

    public override string ToString() => $"<{SystemMessageSender}> {Content}";
}

public class UserMessage : Message
{
    public UserMessage(string sender, string content) : base(content)
    {
        Sender = sender;
    }

    public string Sender { get; }

    public override string ToString() => $"[{Sender}] {Content}";
}

public class Message
{
    public Message(string content)
    {
        Content = content;
        TimeStamp = DateTime.Now;
    }

    public string Content { get; }
    public DateTime TimeStamp { get; }

    public override string ToString() => Content;
}