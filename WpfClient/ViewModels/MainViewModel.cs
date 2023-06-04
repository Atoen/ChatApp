using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using RestSharp;
using RestSharp.Authenticators;
using WpfClient.Models;

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;

    [ObservableProperty] private ObservableCollection<Message> _messages = new(new[] {new Message("User", "Message") {IsFirstMessage = true}});
    [ObservableProperty] private ObservableCollection<string> _onlineUsers = new();
    [ObservableProperty] private string _username = "Username";
    [ObservableProperty] private string _connectionStatus = "Connecting";
    public IAsyncRelayCommand SendCommand { get; }
    public IAsyncRelayCommand SendFileCommand { get; }

    private readonly Dispatcher _dispatcher;
    private HubConnection _connection = default!;
    private RestClient _restClient = default!;
    private readonly TimeSpan _firstMessageTimeSpan = TimeSpan.FromMinutes(5);

    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
        SendCommand = new AsyncRelayCommand(SendAsync, () => !string.IsNullOrWhiteSpace(Message));
        SendFileCommand = new AsyncRelayCommand(SendFileAsync);
    }

    public void SetToken(string token)
    {
        _restClient = new RestClient(new RestClientOptions
        {
            BaseUrl = new Uri(App.EndPointUri),
            Authenticator = new JwtAuthenticator(token)
        });

        _connection = new HubConnectionBuilder()
            .WithUrl($"{App.EndPointUri}/chat", options =>
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
                    message.TimeStamp.Subtract(lastMessage.TimeStamp) > _firstMessageTimeSpan)
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

    private async Task SendAsync(CancellationToken token = default)
    {
        try
        {
            var message = Message;
            Message = string.Empty;
            
            await _connection.InvokeAsync("SendMessage", message, cancellationToken: token);
        }
        catch (Exception e)
        {
            _dispatcher.Invoke(() => Messages.Add(new Message("System", e.Message)
            {
                IsFirstMessage = true
            }));
        }
    }

    private async Task SendFileAsync(CancellationToken token = default)
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false
        };
        if (fileDialog.ShowDialog() == false) return;
        
        var filePath = fileDialog.FileName;

        var request = new RestRequest("api/File", Method.Post);
        request.AddHeader("Content-Type", "multipart/form-data");
        request.AddFile("file", filePath);

        try
        {
            var response = await _restClient.ExecuteAsync(request, token);
            if (response.IsSuccessStatusCode)
            {
                var uri = JsonSerializer.Deserialize<string>(response.Content!);
                await SendFileDownloadUriAsync(uri!, token);
            }
            else
            {
                _dispatcher.Invoke(() => Messages.Add(new Message("System", "Could not upload the file.")
                {
                    IsFirstMessage = true
                }));
            }
        }
        catch (HttpRequestException e)
        {
            _dispatcher.Invoke(() => Messages.Add(new Message("System", e.Message)
            {
                IsFirstMessage = true
            }));
        }
    }

    private async Task SendFileDownloadUriAsync(string uri, CancellationToken token = default)
    {
        try
        {
            await _connection.InvokeAsync("SendMessage", uri, cancellationToken: token);
        }
        catch (Exception e)
        {
            _dispatcher.Invoke(() => Messages.Add(new Message("System", e.Message)
            {
                IsFirstMessage = true
            }));
        }
    }
}