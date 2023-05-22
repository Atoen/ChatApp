using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Server.Messages;

namespace Server.Net;

public class NetworkReader : BinaryReader
{
    private readonly NetworkStream _stream;

    public NetworkReader(NetworkStream stream) : base(stream, Encoding.Unicode, true)
    {
        _stream = stream;
    }

    public async ValueTask<Packet> ReadPacketAsync() => await ReadAsync<Packet>();

    public async ValueTask<Message> ReadMessageAsync() => await ReadAsync<Message>();

    private async ValueTask<T> ReadAsync<T>()
    {
        var length = Read7BitEncodedInt();
        var buffer = ArrayPool<byte>.Shared.Rent(length);

        var read = await _stream.ReadAsync(buffer.AsMemory()[..length]);

        if (read != length)
        {
            throw new InvalidOperationException("Could not read data from the network stream.");
        }

        var data = JsonSerializer.Deserialize<T>(buffer.AsSpan()[..length]);

        ArrayPool<byte>.Shared.Return(buffer);

        return data!;
    }
}