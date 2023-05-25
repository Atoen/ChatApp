using System.Text.Json.Serialization;
using Server.Messages;

namespace Server.Net;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(Packet))]
[JsonSerializable(typeof(Message))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}