using System.Net.NetworkInformation;

namespace Server.Packets;

public static class PacketTransmissionExtensions
{
    public static async Task SendMessageAsync(this PacketWriter writer, string message)
    {
        await writer.WriteOpCodeAsync(OpCode.SendMessage);
        await writer.WriteMessageContentAsync(message);
    }

    public static async Task SendNewUserNameAsync(this PacketWriter writer, string username)
    {
        await writer.WriteOpCodeAsync(OpCode.Connect);
        await writer.WriteMessageContentAsync(username);
    }

    public static async Task BroadcastConnectedAsync(this PacketWriter writer, User connectedUser)
    {
        await writer.WriteOpCodeAsync(OpCode.BroadcastConnected);
        await writer.WriteMessageContentAsync(connectedUser.Username);
        await writer.WriteMessageContentAsync(connectedUser.Uid);
    }

    public static async Task BroadcastDisconnectedAsync(this PacketWriter writer, User disconnectedUser)
    {
        await writer.WriteOpCodeAsync(OpCode.BroadcastDisconnected);
        await writer.WriteMessageContentAsync(disconnectedUser.Username);
        await writer.WriteMessageContentAsync(disconnectedUser.Uid);
    }

    public static async Task<(string username, string message)> GetMessageAsync(this PacketReader reader)
    {
        var username = await reader.ReadMessageAsync();
        var message = await reader.ReadMessageAsync();

        return (username, message);
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
}