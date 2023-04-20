using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Server.Packets;

public class PacketReader : BinaryReader
{
    public PacketReader(NetworkStream networkStream) : base(networkStream, Encoding.UTF8, true)
    {
        _stream = networkStream;
    }

    private readonly NetworkStream _stream;

    public OpCode ReadOpCode()
    {
        var read = _stream.ReadByte();

        if (read != 1) throw new NetworkInformationException();

        return (OpCode) read;
    }

    public string ReadMessage()
    {
        var length = ReadInt32();
        var buffer = ArrayPool<byte>.Shared.Rent(length);

        var read = _stream.Read(buffer, 0, length);
        var message = Encoding.UTF8.GetString(buffer, 0, read);

        ArrayPool<byte>.Shared.Return(buffer);

        return message;
    }

    public async Task<OpCode> ReadOpCodeAsync()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1);

        var read = await _stream.ReadAsync(buffer.AsMemory(0, 1));
        if (read != 1) throw new NetworkInformationException();

        var code = (OpCode) buffer[0];
        ArrayPool<byte>.Shared.Return(buffer);

        return code;
    }

    public async Task<string> ReadMessageAsync()
    {
        var length = ReadInt32();

        var buffer = ArrayPool<byte>.Shared.Rent(length);

        var read = await _stream.ReadAsync(buffer.AsMemory(0, length));
        var message = Encoding.UTF8.GetString(buffer, 0, read);

        ArrayPool<byte>.Shared.Return(buffer);

        return message;
    }
}
