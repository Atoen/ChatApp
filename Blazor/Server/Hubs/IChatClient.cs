﻿
using Blazor.Server.Models;

namespace Blazor.Server.Hubs;

public interface IChatClient
{
    Task ReceiveMessage(Message message);

    Task UserConnected(string user);

    Task UserDisconnected(string user);

    Task GetConnectedUsers(IEnumerable<string> users);
}