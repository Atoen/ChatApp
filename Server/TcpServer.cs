using System.Net;
using System.Net.Sockets;
using Serilog;
using Server.Commands;
using Server.Messages;
using Server.Net;
using Server.Users;

namespace Server;

public class TcpServer
{
    public TcpServer(IPEndPoint endPoint, ICommandHandler commandHandler)
    {
        _listener = new TcpListener(endPoint);
        _commandHandler = commandHandler;
    }

    private readonly ICommandHandler _commandHandler;

    private readonly TcpListener _listener;
    private readonly List<TcpUser> _connectedUsers = new();

    private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

    public async Task Start()
    {
        _listener.Start();

        Log.Information("Server listening on {Address}", _listener.Server.LocalEndPoint?.ToString());

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync();
            Log.Debug("Accepted connection from {Remote}", client.Client.RemoteEndPoint?.ToString());
            try
            {
                var user = await ConnectUser(client);
                user.StartListening();
                user.TransmissionReceived += ConnectionOnTransmissionReceived;
                _connectedUsers.Add(user);

                await BroadcastConnectedUser(user);
            }
            catch (Exception e)
            {
                client.Close();
                Log.Error("Error while accepting new user: {Error}", e.Message);
            }
        }
    }

    private async Task ConnectionOnTransmissionReceived(TcpUser tcpUser, Packet packet)
    {
        switch (packet.OpCode)
        {
            case OpCode.Disconnect:
                await UserDisconnected(tcpUser);
                break;

            case OpCode.SendMessage:
                await MessageReceived(tcpUser);
                break;
        }
    }

    private async Task MessageReceived(TcpUser tcpUser)
    {
        var message = await tcpUser.ReadMessageAsync();
        var content = message.Content;

        Log.Information("<{Username}> \"{UserMessage}\"", tcpUser.Username, content);

        if (message.Content.StartsWith(_commandHandler.Prefix))
        {
            await _commandHandler.Handle(tcpUser, content[1..]);
            return;
        }

        await BroadcastMessage(message);
    }

    private async Task<TcpUser> ConnectUser(TcpClient client)
    {
        using var reader = new NetworkReader(client.GetStream());
        var packet = await reader.ReadPacketAsync();

        if (packet.OpCode != OpCode.Connect)
        {
            throw new InvalidOperationException("Connected client didn't complete the handshake.");
        }

        var username = packet[0];

        ValidateUsername(username, out var validated);
        var uid = Guid.NewGuid();

        await using var writer = new NetworkWriter(client.GetStream());
        var response = new Packet(OpCode.Connect, validated, uid.ToString());
        await writer.WritePacketAsync(response);

        Log.Information("User {Username} has connected", validated);

        return new TcpUser(client, validated, uid);
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

    private async Task UserDisconnected(TcpUser user)
    {
        Log.Information("User {Username} has disconnected", user.Username);

        _connectedUsers.Remove(user);

        await BroadcastDisconnectedUser(user);

        user.Dispose();
    }

    private async Task BroadcastMessage(Message message)
    {
        foreach (var user in _connectedUsers)
        {
            await user.WriteMessageAsync(message);
        }
    }

    private async Task BroadcastConnectedUser(User connectedUser)
    {
        var packet = new Packet(OpCode.BroadcastConnected, connectedUser.Username, connectedUser.UidString);

        foreach (var user in _connectedUsers)
        {
            await user.WritePacketAsync(packet);
        }
    }

    private async Task BroadcastDisconnectedUser(User disconnectedUser)
    {
        var packet = new Packet(OpCode.BroadcastDisconnected, disconnectedUser.Username, disconnectedUser.UidString);

        foreach (var user in _connectedUsers)
        {
            await user.WritePacketAsync(packet);
        }
    }
}