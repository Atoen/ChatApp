using System.Diagnostics;
using System.Net.Sockets;
using OneOf;
using OneOf.Types;
using Server.Messages;
using Server.Net;
using Server.Users;
using System;

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
    private NetworkWriter _writer = default!;

    public async Task<OneOf<Success<string>, Error<string>>> ConnectToServerAsync(string username)
    {
        if (_client.Connected) return new Error<string>("Client already connected.");

        try
        {
            await _client.ConnectAsync("", 13000).ConfigureAwait(false);
            _stream = _client.GetStream();

            _writer = new NetworkWriter(_stream);
            _reader = new NetworkReader(_stream);

            var packet = new Packet(OpCode.Connect, username);
            await _writer.WritePacketAsync(packet).ConfigureAwait(false);

            var response = await _reader.ReadPacketAsync().ConfigureAwait(false);
            if (response.OpCode != OpCode.Connect)
            {
                return new Error<string>("Server didn't complete the handshake.");
            }

            User = new User(response[0], Guid.Parse(response[1]));

            return new Success<string>(User.Username);
        }
        catch (SocketException e)
        {
            return new Error<string>(e.Message);
        }
        catch (IOException)
        {
            return new Error<string>("Remote host refused the connection.");
        }
    }

    public void Listen() => Task.Run(ProcessIncomingPackets);

    public async Task SendMessageAsync(string message)
    {
        await _writer.WritePacketAsync(new Packet(OpCode.TransferMessage)).ConfigureAwait(false);
        await _writer.WriteMessageAsync(new Message(User, message)).ConfigureAwait(false);
    }

    public async Task CloseAsync()
    {
        if (!_client.Connected) return;

        await _writer.WritePacketAsync(new Packet(OpCode.Disconnect)).ConfigureAwait(false);
        await _client.Client.DisconnectAsync(true).ConfigureAwait(false);
    }

    private async Task ProcessIncomingPackets()
    {
        while (_client.Connected)
        {
            Packet packet;
            try
            {
                packet = await _reader.ReadPacketAsync().ConfigureAwait(false);
            }
            catch (IOException e) // Client was closed while awaiting the read
            {
                Debug.WriteLine(e);
                return;
            }

            switch (packet.OpCode)
            {
                case OpCode.TransferMessage:
                // case OpCode.ReceiveMessage:
                var message = await _reader.ReadMessageAsync().ConfigureAwait(false);
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
                
                case OpCode.Error:
                    NotificationReceived?.Invoke(this, packet[0]);
                    break;
            }
        }
    }
}
