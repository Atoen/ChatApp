using System.Net;
using System.Net.Sockets;
using Serilog;
using Serilog.Core;
using Server.Commands;
using Server.Packets;

namespace Server;

public class TcpServer
{
    public TcpServer(IPEndPoint endPoint)
    {
        _listener = new TcpListener(endPoint);
        _commandHandler = new CommandHandler(this, '/');
    }

    public TcpServer(IPAddress address, int port)
    {
        _listener = new TcpListener(address, port);
        _commandHandler = new CommandHandler(this, '/');
    }

    private readonly ICommandHandler _commandHandler;

    private readonly TcpListener _listener;
    internal List<User> ConnectedUsers { get; } = new();

    public async Task Start()
    {
        _listener.Start();

        Log.Information("Server listening on {Address}", _listener.Server.LocalEndPoint?.ToString());

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Log.Debug("Accepted connection from {Remote}", client.Client.RemoteEndPoint?.ToString());

            var user = await ConnectUser(client);
            user.TransmissionReceived += ConnectionOnTransmissionReceived;
            user.StartListening();

            ConnectedUsers.Add(user);
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
                await MessageReceived(user);
                break;

            case OpCode.BroadcastConnected:
            case OpCode.ReceiveMessage:
            case OpCode.BroadcastDisconnected:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(opCode), opCode, null);
        }
    }

    private async Task MessageReceived(User user)
    {
        var message = await user.Reader.ReadMessageAsync();
        Log.Information("<{Username}> \"{UserMessage}\"", user.Username, message);

        if (message.StartsWith(_commandHandler.Prefix))
        {
            await _commandHandler.Handle(user, message[1..]);
            return;
        }
        
        await BroadcastMessage(user.Username, message);
    }

    private async Task<User> ConnectUser(TcpClient client)
    {
        using var packetReader = new PacketReader(client.GetStream());
        var username = await packetReader.GetNewUserNameAsync();
        
        ValidateUsername(username, out var validated);
        var uid = Guid.NewGuid().ToString();

        Log.Information("User {Username} has connected", validated);

        return new User(client, validated, uid);
    }
    
    private void ValidateUsername(string newUsername, out string validated)
    {
        validated = newUsername;
        
        if (ConnectedUsers.All(user => user.Username != newUsername)) return;

        var suffix = 1;
        do
        {
            newUsername = $"{newUsername}_{suffix++}";
        } while (ConnectedUsers.Any(user => user.Username == newUsername));

        validated = newUsername;
    }

    private async Task UserDisconnected(User user)
    {
        Log.Information("User {Username} has disconnected", user.Username);

        ConnectedUsers.Remove(user);
        await BroadcastDisconnectedUser(user);

        user.Dispose();
    }

    internal async Task BroadcastMessage(string sender, string message)
    {
        foreach (var user in ConnectedUsers)
        {
            await user.Writer.WriteOpCodeAsync(OpCode.ReceiveMessage);
            await user.Writer.WriteMessageContentAsync(sender);
            await user.Writer.WriteMessageContentAsync(message);
        }
    }

    private async Task BroadcastConnectedUser(User connected)
    {
        foreach (var user in ConnectedUsers)
        {
            await user.Writer.BroadcastConnectedAsync(connected);
        }
    }

    private async Task BroadcastDisconnectedUser(User disconnected)
    {
        foreach (var user in ConnectedUsers)
        {
            await user.Writer.BroadcastDisconnectedAsync(disconnected);
        }
    }

    internal async Task Respond(User user, string response)
    {
        await user.Writer.WriteOpCodeAsync(OpCode.ReceiveMessage);
        await user.Writer.WriteMessageContentAsync(SystemMessage.SystemMessageSender);
        await user.Writer.WriteMessageContentAsync(response);
    }
}