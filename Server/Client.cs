using System.Net.Sockets;
using Server.Packets;

namespace Server;

public class Client
{
    public Client()
    {
        _client = new TcpClient();
    }

    private readonly TcpClient _client;
    private PacketReader _packetReader = default!;
    private NetworkStream _stream = default!;

    public async Task ConnectToServerAsync(string username)
    {
        if (_client.Connected) return;
        
        await _client.ConnectAsync("127.0.0.1", 13000);
        _stream = _client.GetStream();
        
        _packetReader = new PacketReader(_stream);

        await using var connectedPacket = new Packet();
        connectedPacket.WriteOpCode(OpCode.Connect);
        await connectedPacket.WriteMessageAsync(username);

        await _stream.WriteAsync(connectedPacket.Bytes);
        
        await ProcessIncomingPackets();
    }

    public async Task SendMessageAsync(string message)
    {
        await using var packet = new Packet();
        packet.WriteOpCode(OpCode.SendMessage);
        await packet.WriteMessageAsync(message);

        await _stream.WriteAsync(packet.Bytes);
    }

    public void Close()
    {
        using var packet = new Packet();
        packet.WriteOpCode(OpCode.Disconnect);
        
        _stream.Write(packet.Bytes);

        _client.Close();
    }

    private async Task ProcessIncomingPackets()
    {
        while (true)
        {
            var opCode = await _packetReader.ReadOpCodeAsync();

            switch (opCode)
            {
                case OpCode.Connect:
                    break;
                
                case OpCode.Disconnect:
                    break;
                
                case OpCode.SendMessage:
                    break;
                
                case OpCode.BroadcastConnected:
                    var username = await _packetReader.ReadMessageAsync();
                    var uid = await _packetReader.ReadMessageAsync();
                    
                    var user = new { username, uid };
                    Console.Title = $"{user} connected!";
                    
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}