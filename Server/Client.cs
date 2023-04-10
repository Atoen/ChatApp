using System.Diagnostics;
using System.Net.Sockets;
using Server.Packets;

namespace Server;

public class Client
{
    public Client()
    {
        _client = new TcpClient();
    }

    private TcpClient _client;
    private PacketReader _packetReader = default!;
    private PacketWriter _packetWriter = default!;
    private NetworkStream _stream = default!;

    private bool _shouldOpenNewConnection;

    public async Task ConnectToServerAsync(string username)
    {
        if (_shouldOpenNewConnection)
        {
            _client = new TcpClient();
            _shouldOpenNewConnection = false;
        }

        if (_client.Connected) return;

        Console.Title = "Connecting...";

        try
        {
            await _client.ConnectAsync("192.168.100.8", 13000);
            _stream = _client.GetStream();
        }
        catch (SocketException e)
        {
            Console.Title = e.Message;
            return;
        }

        _packetReader = new PacketReader(_stream);
        _packetWriter = new PacketWriter(_stream);

        await _packetWriter.SendNewUserNameAsync(username);

        await ProcessIncomingPackets();
    }

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
                    var user = await _packetReader.ReceiveBroadcastConnectedAsync();
                    Console.Title = $"{user} connected!";
                    break;

                case OpCode.BroadcastDisconnected:
                    var u = await _packetReader.ReceiveConnectionConfirmationAsync();
                    Console.Title = $"{u} disconnected!";
                    break;

                case OpCode.ReceiveMessage:
                    break;

                case OpCode.ConfirmConnection:
                    var me = await _packetReader.ReceiveConnectionConfirmationAsync();
                    Console.Title = $"Connected as {me}";
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
            }
        }
    }
}