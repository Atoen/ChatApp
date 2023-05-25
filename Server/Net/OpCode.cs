namespace Server.Net;

public enum OpCode : byte
{
    Connect = 1,
    Disconnect,
    TransferMessage,
    BroadcastConnected,
    BroadcastDisconnected,
    Error,
    TransferFile
}
