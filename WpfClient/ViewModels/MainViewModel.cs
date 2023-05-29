using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _messages = new();
    public IAsyncRelayCommand SendCommand { get; }

    [ObservableProperty] private string _username = string.Empty;

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
            .WithUrl("https://localhost:7141/chat", options =>
            {
                options.Headers["Authorization"] = $"Bearer {token}";
            })
            .WithAutomaticReconnect()
            .Build();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = jwt.Claims.FirstOrDefault(x => x.Type == "name");

        Username = nameClaim?.Value ?? "User";
    }

    public async Task ConnectAsync()
    {
        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            _dispatcher.Invoke(() => Messages.Add($"{user}: {message}"));
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception e)
        {
            _dispatcher.Invoke(() => Messages.Add(e.Message));
        }
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
            _dispatcher.Invoke(() => Messages.Add(e.Message));
        }
    }
}