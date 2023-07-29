using System.ComponentModel.DataAnnotations.Schema;
using Blazor.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Blazor.Server.Models;

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

    public EmbedDto ToDto()
    {
        return new EmbedDto
        {
            Type = Type,
            Data = Data
        };
    }
}

//public enum EmbedType
//{
//    File,
//    Image,
//    Gif
//}