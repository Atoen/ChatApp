using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using RestSharp;
using RestSharp.Authenticators;
using WpfClient.Models;

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;

    [ObservableProperty] private ObservableCollection<Message> _messages = new();
    [ObservableProperty] private ObservableCollection<string> _onlineUsers = new();
    [ObservableProperty] private string _username = "Username";
    [ObservableProperty] private string _connectionStatus = "Connecting";
    public IAsyncRelayCommand SendCommand { get; }

    private readonly Dispatcher _dispatcher;
    private HubConnection _connection = default!;
    private RestClient _restClient = default!;
    
    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
        SendCommand = new AsyncRelayCommand(SendAsync, () => !string.IsNullOrWhiteSpace(Message));
    }

    public void SetToken(string token)
    {
        _restClient = new RestClient(options =>
        {
            options.Authenticator = new JwtAuthenticator(token);
        });

        _connection = new HubConnectionBuilder()
            .WithUrl("https://squadtalk.azurewebsites.net/chat", options =>
            {
                options.Headers["Authorization"] = $"Bearer {token}";
            })
            .WithAutomaticReconnect()
            .Build();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = jwt.Claims.FirstOrDefault(x => x.Type == "unique_name");

        Username = nameClaim?.Value ?? "User";
    }

    public async Task ConnectAsync()
    {
        RegisterHandlers();

        try
        {
            await _connection.StartAsync();
            ConnectionStatus = "Online";
        }
        catch (Exception e)
        {
            _dispatcher.Invoke(() => Messages.Add(new Message("System", e.Message)));
        }
    }

    private void RegisterHandlers()
    {
        _connection.Reconnecting += _ =>
        {
            ConnectionStatus = "Reconnecting";
            return Task.CompletedTask;
        };
        
        _connection.Reconnected += _ =>
        {
            ConnectionStatus = "Online";
            return Task.CompletedTask;
        };
        
        _connection.Closed += async _ =>
        {
            ConnectionStatus = "Disconnected";
            await Task.Delay(TimeSpan.FromSeconds(5));
            await _connection.StartAsync();
        };
        
        _connection.On<IEnumerable<string>>("GetConnectedUsers", users =>
        {
            _dispatcher.Invoke(() =>
            {
                OnlineUsers.Clear();

                foreach (var user in users)
                {
                    OnlineUsers.Add(user);
                }
            });
        });

        _connection.On<Message>("ReceiveMessage", message =>
        {
            _dispatcher.Invoke(() =>
            {
                if (Messages.Count == 0)
                {
                    message.IsFirstMessage = true;
                    Messages.Add(message);
                    return;
                }
                
                var lastMessage = Messages[^1];
                if (message.Author != lastMessage.Author ||
                    message.TimeStamp.Subtract(lastMessage.TimeStamp) > TimeSpan.FromMinutes(1))
                {
                    message.IsFirstMessage = true;
                }
                
                Messages.Add(message);
            });
        });
        
        _connection.On<string>("UserConnected", user =>
        {
            _dispatcher.Invoke(() => OnlineUsers.Add(user));
        });

        _connection.On<string>("UserDisconnected", user =>
        {
            _dispatcher.Invoke(() => OnlineUsers.Remove(user));
        });
    }

    private async Task SendAsync()
    {
        try
        {
            await _connection.InvokeAsync("SendMessage", Message);
            Message = string.Empty;
        }
        catch (Exception e)
        {
            _dispatcher.Invoke(() => Messages.Add(new Message("System", e.Message)));
        }
    }
}