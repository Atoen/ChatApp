using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Server.Messages;

namespace Server.Net;

public class NetworkReader : BinaryReader
{
    private readonly NetworkStream _stream;
    private readonly JsonSerializerOptions _serializerOptions = new();
    private readonly SemaphoreSlim _semaphore = new(1);

    public NetworkReader(NetworkStream stream) : base(stream, Encoding.Unicode, true)
    {
        _stream = stream;
        _serializerOptions.AddContext<SourceGenerationContext>();
    }

    public async Task<Packet> ReadPacketAsync() => await ReadAsync<Packet>().ConfigureAwait(false);

    public async Task<Message> ReadMessageAsync() => await ReadAsync<Message>().ConfigureAwait(false);

    private async Task<T> ReadAsync<T>()
    {
        await _semaphore.WaitAsync();

        try
        {
            var length = Read7BitEncodedInt();
            var buffer = ArrayPool<byte>.Shared.Rent(length);

            var read = await _stream.ReadAsync(buffer.AsMemory()[..length]).ConfigureAwait(false);

            if (read != length)
            {
                throw new InvalidOperationException("Could not read data from the network stream.");
            }

            var data = JsonSerializer.Deserialize<T>(buffer.AsSpan()[..length], _serializerOptions);

            ArrayPool<byte>.Shared.Return(buffer);

            return data!;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}