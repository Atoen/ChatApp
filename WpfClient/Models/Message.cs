using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;

namespace WpfClient.Models;

public class Message
{
    public User Author { get; set; }
    public string Content { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    [JsonIgnore] public bool IsFirstMessage { get; set; }
    [JsonIgnore] public UIElement? UiEmbed { get; set; }
    public Embed? Embed { get; set; }

    public Message(User author, string content)
    {
        Author = author;
        Content = content;
        Timestamp = DateTimeOffset.Now;
    }

    public static Message SystemMessage(string content) => new(User.System, content) {IsFirstMessage = true};
}

public class Embed
{
    public EmbedType Type { get; set; }
    public Dictionary<string, string> Data { get; set; }

    public string this[string key]
    {
        get => Data[key];
        set => Data[key] = value;
    }
}

public enum EmbedType
{
    File,
    Image,
    Gif
}
