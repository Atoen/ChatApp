using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
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
using TusDotNetClient;
using WpfClient.Models;
using WpfClient.Views;
using WpfClient.Views.UserControls;

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;

    [ObservableProperty] private ObservableCollection<Message> _messages = new();
    [ObservableProperty] private ObservableCollection<string> _onlineUsers = new();
    [ObservableProperty] private string _username = "Username";
    [ObservableProperty] private string _connectionStatus = "Connecting";
    public IAsyncRelayCommand SendCommand { get; }
    public IAsyncRelayCommand SendFileCommand { get; }

    private readonly Dispatcher _dispatcher;
    private HubConnection _connection = default!;
    private RestClient _restClient = default!;
    private readonly TusClient _tusClient = new();
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
                message.ParseEmbedData();
                
                if (Messages.Count == 0)
                {
                    message.IsFirstMessage = true;
                    Messages.Add(message);
                    return;
                }

                var lastMessage = Messages[^1];
                if (message.Author != lastMessage.Author ||
                    message.Timestamp.Subtract(lastMessage.Timestamp) > _firstMessageTimeSpan)
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
            AddMessage(new("System", e.Message) {IsFirstMessage = true});
        }
    }

    [ObservableProperty] private double _uploadProgress;
    [ObservableProperty] private bool _isUploading;

    private async Task SendFileAsync(CancellationToken token = default)
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false
        };
        if (fileDialog.ShowDialog() == false) return;

        var fileInfo = new FileInfo(fileDialog.FileName);
        var url = await _tusClient.CreateAsync($"{App.EndPointUri}/tus", fileInfo, ("name", fileInfo.Name));
        var upload = _tusClient.UploadAsync(url, fileInfo, chunkSize:10, cancellationToken: token);

        upload.Progressed += (transferred, total) => UploadProgress = (double) transferred / total;

        try
        {
            IsUploading = true;
            var responses = await upload;

            var uri = responses[^1].Headers.First(x => x.Key == "Url").Value;
            await SendFileEmbed(fileInfo,$"{App.EndPointUri}{uri}", token);
        }
        catch (Exception e)
        {
            AddMessage(new("System", e.Message) {IsFirstMessage = true});
        }

        IsUploading = false;
    }

    private async Task SendFileEmbed(FileInfo fileInfo, string uri, CancellationToken token = default)
    {
        try
        {
            var message = new Message(Username, string.Empty)
            {
                Embed = CreateEmbed(fileInfo, uri)
            };

            await _connection.InvokeAsync("SendMessage2", message, cancellationToken: token);
        }
        catch (Exception e)
        {
            AddMessage(new("System", e.Message) {IsFirstMessage = true});
        }
    }

    private Embed CreateEmbed(FileInfo file, string uri)
    {
        if (file.Extension.ToLower() is ".jpg" or ".jpeg" or ".png")
        {
            return new Embed
            {
                Type = EmbedType.Image,
                EmbedData = new()
                {
                    {"Uri", uri}
                }
            };
        }

        return new Embed
        {
            Type = EmbedType.File,
            EmbedData = new()
            {
                {"Uri", uri},
                {"FileName", file.Name},
                {"FileSize", file.Length.ToString()}
            }
        };
    }

    private void AddMessage(Message message) => _dispatcher.Invoke(() => Messages.Add(message));
}