using System;

namespace WpfClient.Model;

public class Message
{
    public User Author { get; }
    public string Content { get; }
    public DateTimeOffset TimeStamp { get; }

    public Message(User author, string content)
    {
        Author = author;
        Content = content;
        TimeStamp = DateTimeOffset.Now;
    }
}