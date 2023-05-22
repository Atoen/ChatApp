using System.Net.Sockets;
using Server.Messages;
using Server.Net;

namespace Server.Users;

public sealed class TcpUser : User, IDisposable
{
    public TcpUser(TcpClient client, string username, Guid uid) : base(username, uid)
    {
        _client = client;

        var stream = client.GetStream();

        _reader = new NetworkReader(stream);
        _writer = new NetworkWriter(stream);
    }

    public delegate Task TransmissionReceivedEventHandler(TcpUser tcpUser, Packet packet);
    public TransmissionReceivedEventHandler? TransmissionReceived;

    private readonly TcpClient _client;

    private readonly NetworkReader _reader;
    private readonly NetworkWriter _writer;

    public void StartListening() => Task.Run(Listen);

    private async Task Listen()
    {
        while (_client.Connected)
        {
            var packet = await _reader.ReadPacketAsync();
            await TransmissionReceived?.Invoke(this, packet)!;
        }
    }

    public async Task<Message> ReadMessageAsync() => await _reader.ReadMessageAsync();

    public async Task WritePacketAsync(Packet packet) => await _writer.WritePacketAsync(packet);

    public async Task WriteMessageAsync(Message message)
    {
        var header = new Packet(OpCode.SendMessage);

        await _writer.WritePacketAsync(header);
        await _writer.WriteMessageAsync(message);
    }

    public void Dispose()
    {
        _client.Close();
        _reader.Dispose();
        _writer.Dispose();
    }
}