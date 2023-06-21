using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace HttpServer.Models;

[Owned]
public class Embed
{
    // public Guid MessageId { get; set; }
    public EmbedType Type { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DataJson { get; set; } = default!;

    [NotMapped]
    public Dictionary<string, string> Data
    {
        get => JsonConvert.DeserializeObject<Dictionary<string, string>>(DataJson)!;
        set => DataJson = JsonConvert.SerializeObject(value);
    }
}

public enum EmbedType
{
    File,
    Image,
    Gif
}