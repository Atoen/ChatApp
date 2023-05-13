using System.Net.Sockets;
using Server.Packets;

namespace Server;

public sealed class User : IDisposable
{
    public User(TcpClient client, string username, string uid)
    {
        _client = client;

        var stream = client.GetStream();

        Reader = new PacketReader(stream);
        Writer = new PacketWriter(stream);

        Username = username;
        Uid = uid;
    }

    public delegate Task TransmissionReceivedEventHandler(User user, OpCode opCode);
    public event TransmissionReceivedEventHandler? TransmissionReceived;

    public string Username { get; }
    public string Uid { get; }

    private readonly TcpClient _client;
    public PacketReader Reader { get; }
    public PacketWriter Writer { get; }

    public void StartListening() => Task.Run(Listen);

    private async Task Listen()
    {
        while (_client.Connected)
        {
            var opCode = await Reader.ReadOpCodeAsync();
            TransmissionReceived?.Invoke(this, opCode);
        }
    }

    public async Task<string> ReadTransmissionAsync() => await Reader.ReadMessageAsync();

    public async Task SendOpCodeAsync(OpCode opCode) => await Writer.WriteOpCodeAsync(opCode);

    public async Task SendTransmissionAsync(string content) => await Writer.WriteMessageContentAsync(content);

    public void Dispose()
    {
        _client.Close();
        Reader.Dispose();
    }
}