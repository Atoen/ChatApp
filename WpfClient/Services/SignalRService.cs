using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentResults;
using Microsoft.AspNetCore.SignalR.Client;
using WpfClient.Models;

namespace WpfClient.Services;

public class SignalRService
{
    private readonly HubConnection _connection;
    private readonly MessageService _messageService;
    private readonly Dispatcher _dispatcher;

    public Action<string>? ConnectionStatusChanged;
    public Action<IEnumerable<string>>? GetConnectedUsers;
    public Action<string>? UserConnected;
    public Action<string>? UserDisconnected;

    public SignalRService(Func<string> tokenProvider, MessageService messageService, Dispatcher dispatcher)
    {
        _messageService = messageService;
        _dispatcher = dispatcher;
        _connection = new HubConnectionBuilder()
            .WithUrl($"{App.EndPointUri}/chat",
                options => { options.AccessTokenProvider = () => Task.FromResult<string?>(tokenProvider.Invoke()); })
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task<Result> ConnectAsync()
    {
        RegisterHandlers();

        try
        {
            await _connection.StartAsync();
            ConnectionStatusChanged?.Invoke("Online");
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }

        return Result.Ok();
    }

    public async Task<Result> SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await _connection.InvokeAsync("SendMessage", message, cancellationToken);
        }
        catch (Exception e)
        {
            return Result.Fail(e.Message);
        }

        return Result.Ok();
    }

    private void RegisterHandlers()
    {
        _connection.Reconnecting += _ =>
        {
            _dispatcher.Invoke(() => ConnectionStatusChanged?.Invoke("Reconnecting"));
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            _dispatcher.Invoke(() => ConnectionStatusChanged?.Invoke("Online"));
            return Task.CompletedTask;
        };

        _connection.Closed += async _ =>
        {
            _dispatcher.Invoke(() => ConnectionStatusChanged?.Invoke("Disconnected"));
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };

        _connection.On<Message>("ReceiveMessage", message =>
            _messageService.MessageReceivedInternal.Invoke(message));
        
        _connection.On<IEnumerable<string>>("GetConnectedUsers", users =>
            _dispatcher.Invoke(() => GetConnectedUsers?.Invoke(users)));

        _connection.On<string>("UserConnected", user =>
            _dispatcher.Invoke(() => UserConnected?.Invoke(user)));

        _connection.On<string>("UserDisconnected", user =>
            _dispatcher.Invoke(() => UserDisconnected?.Invoke(user)));

    }
}