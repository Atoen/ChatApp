using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Server.Packets;

namespace Server;

public delegate Task TransmissionReceivedEventHandler(User user, OpCode opCode);

public class User
{
    public User(TcpClient client, string username, string uid)
    {
        _client = client;
        _packetReader = new PacketReader(client.GetStream());

        Username = username;
        Uid = uid;
    }

    public event TransmissionReceivedEventHandler? TransmissionReceived;
    
    public string Username { get; }
    public string Uid { get; }

    private readonly TcpClient _client;
    private readonly PacketReader _packetReader;

    public void StartListening() => Task.Run(Listen);

    private async Task Listen()
    {
        while (_client.Connected)
        {
            var opCode = await _packetReader.ReadOpCodeAsync();
            TransmissionReceived?.Invoke(this, opCode);
        }
        
        Console.WriteLine($"[{DateTime.Now}]: {_client.Client.RemoteEndPoint} has disconnected!");
    }

    public async Task<string> ReadTransmission() => await _packetReader.ReadMessageAsync();

    public async Task SendTransmission(ReadOnlyMemory<byte> bytes) => await _client.Client.SendAsync(bytes);
}