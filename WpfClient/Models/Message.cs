using System;
using System.Collections.Generic;
using System.Windows;
using WpfClient.Views;
using WpfClient.Views.UserControls;

namespace WpfClient.Models;

public class Message
{
    public string Author { get; }
    public string Content { get; }
    public DateTimeOffset Timestamp { get; }
    public bool IsFirstMessage { get; set; }
    public UIElement? UiEmbed { get; set; }
    public Embed? Embed { get; set; }

    public Message(string author, string content)
    {
        Author = author;
        Content = content;
        Timestamp = DateTimeOffset.Now;
    }

    public void ParseEmbedData()
    {
        if (Embed is {Type: EmbedType.Image})
        {
            UiEmbed = new ImageEmbed
            {
                ImageSource = Embed["Uri"]
            };
        }
        
        else if (Embed is not null)
        {
            UiEmbed = new FileEmbed
            {
                FileType = FileType.Default,
                DownloadUrl = Embed["Uri"],
                FileName = Embed["FileName"],
                FileSize = long.Parse(Embed["FileSize"])
            };
        }
        
        else if (Content.StartsWith("https://tenor.com/"))
        {
            UiEmbed = new GifEmbed
            {
                ImageSource = Content
            };
        }
    }
}

public class Embed
{
    public EmbedType Type { get; set; }
    public Dictionary<string, string> EmbedData { get; set; } = default!;

    public string this[string key]
    {
        get => EmbedData[key];
        set => EmbedData[key] = value;
    }
}

public enum EmbedType
{
    File,
    Image,
    Gif
}
