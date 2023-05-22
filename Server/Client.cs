using System.Diagnostics;
using System.Net.Sockets;
using OneOf;
using OneOf.Types;
using Server.Messages;
using Server.Net;
using Server.Users;

namespace Server;

public class Client
{
    public Client() => _client = new TcpClient();

    public bool Connected => _client.Connected;
    public User User { get; private set; } = default!;
    public event EventHandler<Message>? MessageReceived;
    public event EventHandler<string>? NotificationReceived;

    private readonly TcpClient _client;
    private NetworkStream _stream = default!;

    private NetworkReader _reader = default!;
    private NetworkWriter _networkWriter = default!;

    public async Task<OneOf<Success, Success<string>, Error<string>>> ConnectToServerAsync(string username)
    {
        if (_client.Connected) return new Error<string>("Client already connected.");

        try
        {
            await _client.ConnectAsync("127.0.0.1", 13000);
            _stream = _client.GetStream();
        }
        catch (SocketException e)
        {
            return new Error<string>(e.Message);
        }

        _networkWriter = new NetworkWriter(_stream);
        _reader = new NetworkReader(_stream);

        var packet = new Packet(OpCode.Connect, username);
        await _networkWriter.WritePacketAsync(packet);

        var response = await _reader.ReadPacketAsync();
        if (response.OpCode != OpCode.Connect) return new Error<string>("Server didn't complete the handshake.");

        User = new User(response[0], Guid.Parse(response[1]));

        return username == User.Username
            ? new Success()
            : new Success<string>(User.Username);
    }

    public void Listen() => Task.Run(ProcessIncomingPackets);

    public async Task SendMessageAsync(string message)
    {
        await _networkWriter.WritePacketAsync(new Packet(OpCode.SendMessage));
        await _networkWriter.WriteMessageAsync(new Message(User, message));
    }

    public async Task CloseAsync()
    {
        if (_client.Connected)
        {
            await _networkWriter.WritePacketAsync(new Packet(OpCode.Disconnect));
        }

        await _client.Client.DisconnectAsync(true);
    }

    private async Task ProcessIncomingPackets()
    {
        while (_client.Connected)
        {
            Packet packet;
            try
            {
                packet = await _reader.ReadPacketAsync();
            }
            catch (IOException e) // Client was closed while awaiting the read
            {
                Debug.WriteLine(e);
                return;
            }

            switch (packet.OpCode)
            {
                case OpCode.SendMessage:
                case OpCode.ReceiveMessage:

                    var message = await _reader.ReadMessageAsync();
                    if (message.Author != User)
                    {
                        MessageReceived?.Invoke(this,  message);
                    }
                    break;

                case OpCode.BroadcastConnected:
                    if (packet[1] != User.UidString)
                    {
                        NotificationReceived?.Invoke(this, $"{packet[0]} has connected to the server.");
                    }
                    break;

                case OpCode.BroadcastDisconnected:
                    if (packet[1] != User.UidString)
                    {
                        NotificationReceived?.Invoke(this, $"{packet[0]} has disconnected from the server.");
                    }
                    break;
            }
        }
    }
}
