using System;
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
using FluentResults;
using Microsoft.Win32;
using WpfClient.Extensions;
using WpfClient.Models;
using WpfClient.Services;

namespace WpfClient.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _userMessage = string.Empty;
    [ObservableProperty] private MessageCollection _messages = new();
    [ObservableProperty] private ObservableCollection<string> _onlineUsers = new();
    [ObservableProperty] private string _username = "Username";
    [ObservableProperty] private string _connectionStatus = "Connecting";

    [ObservableProperty] private bool _isUploading;
    [ObservableProperty] private double _uploadProgress;

    private readonly Dispatcher _dispatcher;

    private Message? _uploadMessage;
    private string _token = "token";

    private readonly FileTransferService _fileTransferService;
    private readonly MessageService _messageService;
    private readonly SignalRService _signalRService;

    public MainViewModel()
    {
        _dispatcher = Application.Current.Dispatcher;
        string TokenProvider() => _token;

        _fileTransferService = new FileTransferService(TokenProvider);
        _fileTransferService.UploadingChanged += uploading => IsUploading = uploading;
        _fileTransferService.UploadProgressChanged += progress => UploadProgress = progress;

        _messageService = new MessageService(TokenProvider, _dispatcher)
        {
            MessageReceived = message => Messages.Add(message)
        };

        _signalRService = new SignalRService(TokenProvider, _messageService, _dispatcher)
        {
            ConnectionStatusChanged = status => ConnectionStatus = status,
            UserConnected = user => OnlineUsers.Add(user),
            UserDisconnected = user => OnlineUsers.Remove(user),
            GetConnectedUsers = users =>
            {
                OnlineUsers.Clear();

                foreach (var user in users)
                {
                    OnlineUsers.Add(user);
                }
            }
        };
    }

    private bool CanPasteImage() => !IsUploading;

    public void SetToken(string token)
    {
        _token = token;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var nameClaim = jwt.Claims.FirstOrDefault(x => x.Type == "unique_name");

        Username = nameClaim?.Value ?? throw new InvalidOperationException();
    }

    public async Task ConnectAsync()
    {
        var result = await _signalRService.ConnectAsync();
        ShowErrorsIfFailed(result);

        if (!result.IsFailed)
        {
            var page = await _messageService.GetNextMessagePageFormattedAsync();
            _dispatcher.Invoke(() => Messages.InsertPage(page));
        }
    }

    public async void GetNextPage()
    {
        var page = await _messageService.GetNextMessagePageFormattedAsync();
        _dispatcher.Invoke(() => Messages.InsertPage(page));
    }

    [RelayCommand]
    private async Task SendMessageAsync(CancellationToken token)
    {
        var messageContent = UserMessage;
        UserMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(messageContent)) return;

        var result = await _signalRService.SendMessageAsync(messageContent, token);
        ShowErrorsIfFailed(result);
    }

    [RelayCommand(CanExecute = nameof(CanPasteImage))]
    private async Task PasteImageAsync(CancellationToken cancellationToken)
    {
        var image = Clipboard.GetImage()!;

        var path = Path.Combine(Path.GetTempPath(), "image.png");
        var fileStream = new FileStream(path, FileMode.Create);
        BitmapEncoder encoder = new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(fileStream);

        await fileStream.DisposeAsync();

        var fileInfo = new FileInfo(path);
        await DisplayFileTransferProgress(fileInfo, cancellationToken);

        File.Delete(path);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SendFileAsync(CancellationToken token)
    {
        var fileDialog = new OpenFileDialog
        {
            Multiselect = false
        };
        if (fileDialog.ShowDialog() == false) return;

        var fileInfo = new FileInfo(fileDialog.FileName);

        await DisplayFileTransferProgress(fileInfo, token);
    }

    private async Task DisplayFileTransferProgress(FileInfo fileInfo, CancellationToken token)
    {
        _uploadMessage = _fileTransferService.CreateUploadMessage(fileInfo);
        AddMessage(_uploadMessage);

        var result = await _fileTransferService.UploadFileAsync(Username, fileInfo, token);
        RemoveUploadMessage();

        ShowErrorsIfFailed(result);
    }

    private void ShowErrorsIfFailed(Result result)
    {
        foreach (var error in result.Errors)
        {
            AddMessage(Message.SystemMessage(error.Message));
        }
    }

    private void RemoveUploadMessage()
    {
        if (_uploadMessage is not null) _dispatcher.Invoke(() => Messages.Remove(_uploadMessage));
        _uploadMessage = null;
    }

    private void AddMessage(Message message) => _dispatcher.Invoke(() => Messages.Add(message));
}