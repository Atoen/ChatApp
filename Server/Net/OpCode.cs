namespace Server.Net;

public enum OpCode : byte
{
    Connect = 1,
    Disconnect,
    SendMessage,
    ReceiveMessage,
    BroadcastConnected,
    BroadcastDisconnected,
    Error,
}
