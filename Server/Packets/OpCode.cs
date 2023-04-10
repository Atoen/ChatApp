namespace Server.Packets;

public enum OpCode : byte
{
    Connect = 1,
    Disconnect,
    SendMessage,
    ReceiveMessage,
    BroadcastConnected,
    BroadcastDisconnected,
    ConfirmConnection,
}
