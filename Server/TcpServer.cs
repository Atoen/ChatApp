using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Serilog;
using Serilog.Core;
using Server.Packets;

namespace Server;

public class TcpServer
{
    public TcpServer(IPEndPoint endPoint) => _listener = new TcpListener(endPoint);

    public TcpServer(IPAddress address, int port) => _listener = new TcpListener(address, port);

    private readonly TcpListener _listener;
    private readonly List<User> _connectedUsers = new();
    private readonly Logger _log = new LoggerConfiguration().
        MinimumLevel.Debug().
        WriteTo.Console().
        CreateLogger();

    public async Task Start()
    {
        _listener.Start();
        
        _log.Information("Server listening on {Address}", _listener.Server.LocalEndPoint?.ToString());

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            _log.Debug("Accepted connection from {Remote}", client.Client.RemoteEndPoint?.ToString());
            
            var user = await ConnectUser(client);
            user.TransmissionReceived += ConnectionOnTransmissionReceived;
            user.StartListening();
            
            _connectedUsers.Add(user);

            await BroadcastConnection();
        }
    }

    private async Task ConnectionOnTransmissionReceived(User user, OpCode opCode)
    {
        switch (opCode)
        {
            case OpCode.Connect:
                break;
            case OpCode.Disconnect:
                _log.Information("User {Username} has disconnected", user.Username);
                break;
            case OpCode.SendMessage:
                var message = await user.ReadTransmission();
                _log.Information("<{Username}> \"{Message}\"", user.Username, message);
                break;
            case OpCode.BroadcastConnected:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
        }
    }

    private async Task<User> ConnectUser(TcpClient client)
    {
        var packetReader = new PacketReader(client.GetStream());

        var opCode = await packetReader.ReadOpCodeAsync();
        if (opCode != OpCode.Connect) throw new NetworkInformationException();
        
        var username = await packetReader.ReadMessageAsync();
        var uid = Guid.NewGuid().ToString();
        
        _log.Information("User {Username} has connected", username);
        
        var user = new User(client, username, uid);
        
        return user;
    }

    private async Task BroadcastConnection()
    {
        foreach (var user in _connectedUsers)
        {
            await using var packet = new Packet();
            
            packet.WriteOpCode(OpCode.BroadcastConnected);
            await packet.WriteMessageAsync(user.Username);
            await packet.WriteMessageAsync(user.Uid);

            await user.SendTransmission(packet.Bytes);
        }
    }
}