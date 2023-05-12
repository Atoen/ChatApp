using System.Buffers;
using System.Net.Sockets;
using System.Text;

namespace Server.Packets;

public class PacketWriter : BinaryWriter
{
    public PacketWriter(NetworkStream networkStream) : base(networkStream, Encoding.UTF8, true)
    {
        _stream = networkStream;
    }

    private readonly NetworkStream _stream;

    public void WriteOpCode(OpCode opCode) => _stream.WriteByte((byte) opCode);

    public void WriteMessage(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        _stream.Write(BitConverter.GetBytes(bytes.Length));
        _stream.Write(Encoding.UTF8.GetBytes(content));
    }

    public async Task WriteOpCodeAsync(OpCode opCode)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1);
        buffer[0] = (byte) opCode;

        await _stream.WriteAsync(buffer.AsMemory(0, 1));
    }

    public async Task WriteMessageAsync(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);

        Write7BitEncodedInt(bytes.Length);
        await _stream.WriteAsync(bytes);
    }
}
