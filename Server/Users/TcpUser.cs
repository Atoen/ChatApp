using System.Net.Sockets;
using Serilog;
using Server.Messages;
using Server.Net;

namespace Server.Users;

public sealed class TcpUser : User, IDisposable
{
    public ITcpServer Server { get; }

    public TcpUser(ITcpServer server, TcpClient client, Func<Packet, TcpUser, Task> callback, string username, Guid uid) : base(username, uid)
    {
        Server = server;
        _client = client;
        _callback = callback;

        var stream = client.GetStream();

        _reader = new NetworkReader(stream);
        _writer = new NetworkWriter(stream);
    }
    
    private readonly Func<Packet, TcpUser, Task> _callback;
    private readonly TcpClient _client;
    private readonly NetworkReader _reader;
    private readonly NetworkWriter _writer;
    
    public async Task Listen()
    {
        while (_client.Connected)
        {
            await ProcessPacket().ConfigureAwait(false);
        }
    }

    private async Task ProcessPacket()
    {
        try
        {
            var packet = await _reader.ReadPacketAsync().ConfigureAwait(false);
            await _callback.Invoke(packet, this).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Log.Error("Error while processing user packet: {Error}", e.Message);
            await WritePacketAsync(new Packet(OpCode.Error, e.Message)).ConfigureAwait(false);
        }
    }

    public async Task<Message> ReadMessageAsync() => await _reader.ReadMessageAsync().ConfigureAwait(false);

    public async Task WritePacketAsync(Packet packet) => await _writer.WritePacketAsync(packet).ConfigureAwait(false);

    public async Task WriteMessageAsync(Message message)
    {
        var header = new Packet(OpCode.SendMessage);

        await _writer.WritePacketAsync(header).ConfigureAwait(false);
        await _writer.WriteMessageAsync(message).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _client.Close();
        _reader.Dispose();
        _writer.Dispose();
    }
}