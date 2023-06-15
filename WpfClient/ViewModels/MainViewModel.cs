using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Win32;
using RestSharp;
using RestSharp.Authenticators;
using TusDotNetClient;
using WpfClient.Models;
using WpfClient.Views.UserControls;

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private ObservableCollection<Message> _messages = new();
    [ObservableProperty] private ObservableCollection<string> _onlineUsers = new();
    [ObservableProperty] private string _username = "Username";
    [ObservableProperty] private string _connectionStatus = "Connecting";
    [ObservableProperty] private double _uploadProgress;
    [ObservableProperty] private bool _isUploading;

    private readonly Dispatcher _dispatcher;
    private HubConnection _connection = default!;
    private RestClient _restClient = default!;
    private readonly TusClient _tusClient = new();
    private readonly TimeSpan _firstMessageTimeSpan = TimeSpan.FromMinutes(5);

    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
    }

    [RelayCommand(CanExecute = nameof(CanPasteImage))]
    private async Task PasteImageAsync()
    {
        var image = Clipboard.GetImage()!;

        var path = Path.Combine(Path.GetTempPath(), "image.png");
        var fileStream = new FileStream(path, FileMode.Create);
        BitmapEncoder encoder = new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);

        await fileStream.DisposeAsync();

        var fileInfo = new FileInfo(path);
        await UploadFileAsync(fileInfo);

        File.Delete(path);
    }

    private bool CanPasteImage() => !IsUploading;
    
    public void SetToken(string token)
    {
        _restClient = new RestClient(new RestClientOptions
        {
            BaseUrl = new Uri(App.EndPointUri),
            Authenticator = new JwtAuthenticator(token)
        });

        _tusClient.AdditionalHeaders["Authorization"] = $"Bearer {token}";

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
            _dispatcher.Invoke(() => ReceiveMessage(message));
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

    private void ReceiveMessage(Message message)
    {
        message.ParseEmbedData(_restClient);

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
    }

    [RelayCommand]
    private async Task SendMessageAsync(CancellationToken token = default)
    {
        var messageContent = Message;
        Message = string.Empty;
        
        if (string.IsNullOrWhiteSpace(messageContent)) return;
        
        try
        {
            await _connection.InvokeAsync("SendMessage", messageContent, cancellationToken: token);
        }
        catch (Exception e)
        {
            AddMessage(new("System", e.Message) {IsFirstMessage = true});
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendFileAsync(CancellationToken token = default)
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false
        };
        if (fileDialog.ShowDialog() == false) return;
        
        var fileInfo = new FileInfo(fileDialog.FileName);
        
        _uploadMessage = CreateUploadMessage(fileInfo);
        AddMessage(_uploadMessage);

        await UploadFileAsync(fileInfo, token);
    }

    private Message CreateUploadMessage(FileInfo fileInfo)
    {
        var embed = new FileUploadEmbed
        {
            UploadedFileName = fileInfo.Name,
            UploadedFileSize = fileInfo.Length
        };

        return new Message("System", string.Empty)
        {
            UiEmbed = embed
        };
    }

    private void RemoveUploadMessage()
    {
        if (_uploadMessage is not null) _dispatcher.Invoke(() => Messages.Remove(_uploadMessage));
        _uploadMessage = null;
    }

    private Message? _uploadMessage;

    private async Task UploadFileAsync(FileInfo fileInfo, CancellationToken token = default)
    {
        IsUploading = true;

        try
        {
            var url = await _tusClient.CreateAsync($"{App.EndPointUri}/tus", fileInfo,
                ("length", fileInfo.Length.ToString()), ("sender", Username));

            var upload = _tusClient.UploadAsync(url, fileInfo, chunkSize: 10, cancellationToken: token);

            upload.Progressed += (transferred, total) => UploadProgress = (double) transferred / total;
            await upload;
        }
        catch (Exception e)
        {
            if (!token.IsCancellationRequested)
            {
                AddMessage(new("System", e.Message) {IsFirstMessage = true});
            }
        }
        RemoveUploadMessage();

        IsUploading = false;
    }

    private void AddMessage(Message message) => _dispatcher.Invoke(() => Messages.Add(message));
}