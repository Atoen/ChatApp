using System.Net;
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
            await BroadcastConnectedUser(user);
        }
    }

    private async Task ConnectionOnTransmissionReceived(User user, OpCode opCode)
    {
        switch (opCode)
        {
            case OpCode.Connect:
                break;

            case OpCode.Disconnect:
                await UserDisconnected(user);
                break;

            case OpCode.SendMessage:
                var message = await user.Reader.ReadMessageAsync();
                await SendMessage(user.Username, message);
                _log.Information("<{Username}> \"{Message}\"", user.Username, message);
                break;

            case OpCode.BroadcastConnected:
            case OpCode.ReceiveMessage:
            case OpCode.BroadcastDisconnected:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
        }
    }

    private async Task<User> ConnectUser(TcpClient client)
    {
        using var packetReader = new PacketReader(client.GetStream());

        var username = await packetReader.GetNewUserNameAsync();
        
        ValidateUsername(username, out var validated);
        
        var uid = Guid.NewGuid().ToString();

        _log.Information("User {Username} has connected", validated);

        var user = new User(client, validated, uid);

        return user;
    }
    
    private void ValidateUsername(string newUsername, out string validated)
    {
        validated = newUsername;
        
        if (_connectedUsers.All(user => user.Username != newUsername)) return;

        var suffix = 1;
        do
        {
            newUsername = $"{newUsername}_{suffix++}";
        } while (_connectedUsers.Any(user => user.Username == newUsername));

        validated = newUsername;
    }

    private async Task UserDisconnected(User user)
    {
        _log.Information("User {Username} has disconnected", user.Username);

        _connectedUsers.Remove(user);
        await BroadcastDisconnectedUser(user);

        user.Dispose();
    }

    private async Task SendMessage(string sender, string message)
    {
        foreach (var user in _connectedUsers)
        {
            await user.Writer.WriteOpCodeAsync(OpCode.ReceiveMessage);
            await user.Writer.WriteMessageAsync(sender);
            await user.Writer.WriteMessageAsync(message);
        }
    }

    private async Task BroadcastConnectedUser(User connected)
    {
        foreach (var user in _connectedUsers)
        {
            await user.Writer.BroadcastConnectedAsync(connected);
        }
    }

    private async Task BroadcastDisconnectedUser(User disconnected)
    {
        foreach (var user in _connectedUsers)
        {
            await user.Writer.BroadcastDisconnectedAsync(disconnected);
        }
    }
}