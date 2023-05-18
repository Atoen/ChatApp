using System.Diagnostics;
using System.Net.Sockets;
using OneOf;
using OneOf.Types;
using Server.Packets;

namespace Server;

public class Client
{
    public Client()
    {
        _client = new TcpClient();
    }

    public bool Connected => _client.Connected;
    public event EventHandler<Message>? MessageReceived;

    private TcpClient _client;
    private PacketReader _packetReader = default!;
    private PacketWriter _packetWriter = default!;
    private NetworkStream _stream = default!;

    private bool _shouldOpenNewConnection;

    public async Task<OneOf<Success, Error<string>>> ConnectToServerAsync(string username)
    {
        if (_client.Connected) return new Error<string>("Client already connected.");
        
        if (_shouldOpenNewConnection)
        {
            _client = new TcpClient();
            _shouldOpenNewConnection = false;
        }

        try
        {
            await _client.ConnectAsync("", 13000);
            _stream = _client.GetStream();
        }
        catch (SocketException e)
        {
            return new Error<string>(e.Message);
        }

        _packetReader = new PacketReader(_stream);
        _packetWriter = new PacketWriter(_stream);

        await _packetWriter.SendNewUserNameAsync(username);

        return new Success();
    }

    public void Listen() => Task.Run(ProcessIncomingPackets);

    public async Task SendMessageAsync(string message)
    {
        await _packetWriter.SendMessageAsync(message);
    }

    public void Close()
    {
        if (_client.Connected)
        {
            _packetWriter.WriteOpCode(OpCode.Disconnect);
        }

        _client.Close();
        _shouldOpenNewConnection = true;
    }

    private async Task ProcessIncomingPackets()
    {
        while (_client.Connected)
        {
            OpCode opCode;
            try
            {
                opCode = await _packetReader.ReadOpCodeAsync();
            }
            catch (IOException e) // Client was closed while awaiting the read
            {
                Debug.WriteLine(e);
                return;
            }

            switch (opCode)
            {
                case OpCode.Connect:
                    break;

                case OpCode.Disconnect:
                    break;

                case OpCode.SendMessage:
                    break;

                case OpCode.BroadcastConnected:
                     var (user, _) = await _packetReader.ReceiveBroadcastConnectedAsync();
                     MessageReceived?.Invoke(this, new Message($"{user} has connected to the server."));
                     break;

                case OpCode.BroadcastDisconnected:
                    var (usr, _) = await _packetReader.ReceiveBroadcastDisconnectedAsync();
                    MessageReceived?.Invoke(this, new Message($"{usr} has disconnected from the server."));
                    break;

                case OpCode.ReceiveMessage:
                    var (username, message) = await _packetReader.GetMessageAsync();
                    MessageReceived?.Invoke(this, new UserMessage(username, message));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
            }
        }
    }
}
