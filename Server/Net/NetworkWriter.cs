using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Server.Messages;

namespace Server.Net;

public class NetworkWriter : BinaryWriter
{
    private readonly NetworkStream _stream;
    private readonly JsonSerializerOptions _serializerOptions = new();
    private readonly SemaphoreSlim _semaphore = new(1);

    public NetworkWriter(NetworkStream stream) : base(stream, Encoding.Unicode, true)
    {
        _stream = stream;
        _serializerOptions.AddContext<SourceGenerationContext>();
    }

    public async ValueTask WritePacketAsync(Packet packet) => await WriteAsync(packet).ConfigureAwait(false);

    public async ValueTask WriteMessageAsync(Message message) => await WriteAsync(message).ConfigureAwait(false);

    private async ValueTask WriteAsync<T>(T data)
    {
        await _semaphore.WaitAsync();

        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(data, _serializerOptions);

            Write7BitEncodedInt(bytes.Length);
            await _stream.WriteAsync(bytes).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}