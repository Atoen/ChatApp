using System.Net.NetworkInformation;

namespace Server.Packets;

public static class PacketTransmissionExtensions
{
    public static async Task SendMessageAsync(this PacketWriter writer, string message)
    {
        await writer.WriteOpCodeAsync(OpCode.SendMessage);
        await writer.WriteMessageAsync(message);
    }

    public static async Task SendNewUserNameAsync(this PacketWriter writer, string username)
    {
        await writer.WriteOpCodeAsync(OpCode.Connect);
        await writer.WriteMessageAsync(username);
    }

    public static async Task BroadcastConnectedAsync(this PacketWriter writer, User connectedUser)
    {
        await writer.WriteOpCodeAsync(OpCode.BroadcastConnected);
        await writer.WriteMessageAsync(connectedUser.Username);
        await writer.WriteMessageAsync(connectedUser.Uid);
    }

    public static async Task ConfirmConnectionAsync(this PacketWriter writer, User user)
    {
        await writer.WriteOpCodeAsync(OpCode.ConfirmConnection);
        await writer.WriteMessageAsync(user.Username);
        await writer.WriteMessageAsync(user.Uid);
    }

    public static async Task BroadcastDisconnectedAsync(this PacketWriter writer, User disconnectedUser)
    {
        await writer.WriteOpCodeAsync(OpCode.BroadcastDisconnected);
        await writer.WriteMessageAsync(disconnectedUser.Username);
        await writer.WriteMessageAsync(disconnectedUser.Uid);
    }

    public static async Task<string> GetNewUserNameAsync(this PacketReader reader)
    {
        var opCode = await reader.ReadOpCodeAsync();
        if (opCode != OpCode.Connect) throw new NetworkInformationException();

        var username = await reader.ReadMessageAsync();

        return username;
    }

    public static async Task<(string username, string uid)> ReceiveBroadcastConnectedAsync(this PacketReader reader)
    {
        var username = await reader.ReadMessageAsync();
        var uid = await reader.ReadMessageAsync();

        return (username, uid);
    }

    public static async Task<(string username, string uid)> ReceiveBroadcastDisconnectedAsync(this PacketReader reader)
    {
        var username = await reader.ReadMessageAsync();
        var uid = await reader.ReadMessageAsync();

        return (username, uid);
    }

    public static async Task<(string username, string uid)> ReceiveConnectionConfirmationAsync(this PacketReader reader)
    {
        var username = await reader.ReadMessageAsync();
        var uid = await reader.ReadMessageAsync();

        return (username, uid);
    }
}