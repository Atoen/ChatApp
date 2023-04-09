using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Server.Packets;

public class PacketReader : BinaryReader
{
    public PacketReader(NetworkStream networkStream) : base(networkStream)
    {
        _stream = networkStream;
    }

    private readonly NetworkStream _stream;

    public OpCode ReadOpCode()
    {
        Span<byte> span = stackalloc byte[1];
        var read = _stream.Read(span);

        if (read != 1) throw new NetworkInformationException();

        return (OpCode) span[0];
    }

    public string ReadContent()
    {
        var length = ReadInt32();
        var buffer = ArrayPool<byte>.Shared.Rent(length);

        var read = _stream.Read(buffer, 0, length);
        var message = Encoding.UTF8.GetString(buffer);

        return message[..read];
    }

    public async Task<OpCode> ReadOpCodeAsync()
    {
        var buffer = new byte[1];
        var read = await _stream.ReadAsync(buffer.AsMemory());
        
        if (read != 1) throw new NetworkInformationException();
        
        return (OpCode) buffer[0];
    }

    public async Task<string> ReadMessageAsync()
    {
        var length = ReadInt32();
        var buffer = ArrayPool<byte>.Shared.Rent(length);

        var read = await _stream.ReadAsync(buffer.AsMemory(0, length));

        var message = Encoding.UTF8.GetString(buffer);

        return message[..read];

    }
}