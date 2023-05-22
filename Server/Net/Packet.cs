using System.Text.Json.Serialization;

namespace Server.Net;

public readonly struct Packet
{
    public string[] Args { get; }
    public OpCode OpCode { get; }

    [JsonConstructor]
    public Packet(OpCode opCode, params string[] args)
    {
        Args = args;
        OpCode = opCode;
    }

    public string this[int index] => Args[index];
}