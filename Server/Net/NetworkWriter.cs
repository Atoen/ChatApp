using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Server.Messages;

namespace Server.Net;

public class NetworkWriter : BinaryWriter
{
    private readonly NetworkStream _stream;

    public NetworkWriter(NetworkStream stream) : base(stream, Encoding.Unicode, true)
    {
        _stream = stream;
    }

    public async ValueTask WritePacketAsync(Packet packet) => await WriteAsync(packet).ConfigureAwait(false);

    public async ValueTask WriteMessageAsync(Message message) => await WriteAsync(message).ConfigureAwait(false);

    public async ValueTask WriteAsync<T>(T data)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(data);

        Write7BitEncodedInt(bytes.Length);
        await _stream.WriteAsync(bytes).ConfigureAwait(false);
    }
}