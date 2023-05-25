using System.Collections.Concurrent;

namespace Server.Net;

public class FileTransferManager
{
    public readonly ConcurrentBag<string> ActiveFileTransfers = new();
    public bool IsFileAvailable => !ActiveFileTransfers.IsEmpty;

    public void AddAvailableTransfer()
    {
        
    }
}