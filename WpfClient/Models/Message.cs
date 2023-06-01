using System;

namespace WpfClient.Models;

public class Message
{
    public string Author { get; }
    public string Content { get; }
    public DateTimeOffset TimeStamp { get; }
    public bool IsFirstMessage { get; set; }

    public Message(string author, string content)
    {
        Author = author;
        Content = content;
        TimeStamp = DateTimeOffset.Now;
    }
}