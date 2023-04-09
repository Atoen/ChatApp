using System.Text;

namespace Server.Packets;

public sealed class Packet : IDisposable, IAsyncDisposable
{
    private readonly MemoryStream _stream = new();

    public void WriteOpCode(OpCode opCode) => _stream.WriteByte((byte) opCode);

    public void WriteMessage(string content)
    {
        _stream.Write(BitConverter.GetBytes(content.Length));
        _stream.Write(Encoding.UTF8.GetBytes(content));
    }
    
    public async Task WriteMessageAsync(string content)
    {
        await _stream.WriteAsync(BitConverter.GetBytes(content.Length));
        await _stream.WriteAsync(Encoding.UTF8.GetBytes(content));
    }

    public byte[] Bytes => _stream.ToArray();

    public void Dispose()
    {
        _stream.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}