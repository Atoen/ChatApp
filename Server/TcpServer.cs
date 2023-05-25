using System.Net;
using System.Net.Sockets;
using Serilog;
using Server.Commands;
using Server.Messages;
using Server.Net;
using Server.Users;

namespace Server;

public class TcpServer : ITcpServer
{
    public TcpServer(IPEndPoint endPoint, ICommandHandler commandHandler, FileTransferManager fileTransferManager)
    {
        _listener = new TcpListener(endPoint);
        _commandHandler = commandHandler;
        _fileTransferManager = fileTransferManager;
    }

    private readonly ICommandHandler _commandHandler;
    private readonly FileTransferManager _fileTransferManager;

    private readonly TcpListener _listener;
    private readonly List<TcpUser> _connectedUsers = new();

    private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(10);

    public async Task Start()
    {
        _listener.Start();

        Log.Information("Server listening on {Address}", _listener.Server.LocalEndPoint?.ToString());

        while (true)
        {
            var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            Log.Debug("Accepted connection from {Remote}", client.Client.RemoteEndPoint?.ToString());
            try
            {
                var user = await ConnectUserAsync(client, ConnectionOnTransmissionReceived).ConfigureAwait(false);
                _ = Task.Run(user.Listen);

                _connectedUsers.Add(user);

                await BroadcastConnectedUserAsync(user).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                client.Close();
                Log.Error("Error while accepting new user: {Error}", e.Message);
            }
        }
    }

    private async Task ConnectionOnTransmissionReceived(Packet packet, TcpUser user)
    {
        switch (packet.OpCode)
        {
            case OpCode.Disconnect:
                await UserDisconnected(user).ConfigureAwait(false);
                break;

            case OpCode.TransferMessage:
                await MessageReceived(user).ConfigureAwait(false);
                break;

            case OpCode.TransferFile:
            {
                await BroadcastMessageAsync(Message.ServerBroadcast($"{user.Username} wants to share the {packet[0]} file."));
                break;
            }
        }
    }

    private async Task MessageReceived(TcpUser tcpUser)
    {
        var message = await tcpUser.ReadMessageAsync().ConfigureAwait(false);
        var content = message.Content;

        Log.Information("<{Username}> \"{UserMessage}\"", tcpUser.Username, content);

        if (message.Content.StartsWith(_commandHandler.Prefix))
        {
            await _commandHandler.Handle(tcpUser, content[1..]).ConfigureAwait(false);
            return;
        }
        
        await BroadcastMessageAsync(message).ConfigureAwait(false);
    }

    private async Task<TcpUser> ConnectUserAsync(TcpClient client, Func<Packet, TcpUser, Task> callback)
    {
        using var reader = new NetworkReader(client.GetStream());

        var task = reader.ReadPacketAsync();
        if (await Task.WhenAny(task, Task.Delay(_connectionTimeout)).ConfigureAwait(false) != task)
        {
            throw new InvalidOperationException("Connected client didn't complete the handshake.");
        }

        var packet =  await task.ConfigureAwait(false);

        if (packet.OpCode != OpCode.Connect)
        {
            throw new InvalidOperationException("Invalid connection packet received.");
        }

        var username = packet[0];

        ValidateUsername(username, out var validated);
        var uid = Guid.NewGuid();

        var writer = new NetworkWriter(client.GetStream());
        await using var _ = writer.ConfigureAwait(false);
        var response = new Packet(OpCode.Connect, validated, uid.ToString());
        await writer.WritePacketAsync(response).ConfigureAwait(false);

        Log.Information("User {Username} has connected", validated);

        return new TcpUser(this, client, callback, validated, uid);
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

        await user.WritePacketAsync(new Packet(OpCode.Disconnect));

        await BroadcastDisconnectedUser(user).ConfigureAwait(false);

        user.Dispose();
    }

    public async Task DisconnectUserOnError(TcpUser user)
    {
        Log.Information("User {Username} has lost connection", user.Username);
        _connectedUsers.Remove(user);

        await BroadcastDisconnectedUser(user).ConfigureAwait(false);
        user.Dispose();
    }

    public async Task BroadcastMessageAsync(Message message)
    {
        foreach (var user in _connectedUsers)
        {
            await user.WriteMessageAsync(message).ConfigureAwait(false);
        }
    }

    private async Task BroadcastConnectedUserAsync(User connectedUser)
    {
        var packet = new Packet(OpCode.BroadcastConnected, connectedUser.Username, connectedUser.UidString);

        foreach (var user in _connectedUsers)
        {
            await user.WritePacketAsync(packet).ConfigureAwait(false);
        }
    }

    private async Task BroadcastDisconnectedUser(User disconnectedUser)
    {
        var packet = new Packet(OpCode.BroadcastDisconnected, disconnectedUser.Username, disconnectedUser.UidString);

        foreach (var user in _connectedUsers)
        {
            await user.WritePacketAsync(packet).ConfigureAwait(false);
        }
    }
}