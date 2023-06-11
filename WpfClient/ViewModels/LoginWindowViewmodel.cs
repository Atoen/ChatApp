using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Konscious.Security.Cryptography;
using RestSharp;
using WpfClient.Views.Windows;

namespace WpfClient.ViewModels;

public partial class LoginWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            OnPropertyChanged();
            
            _passwordPlaceholderVisible = string.IsNullOrEmpty(_password);
            OnPropertyChanged(nameof(PasswordPlaceholderVisible));
        }
    }

    public bool PasswordPlaceholderVisible
    {
        get => _passwordPlaceholderVisible;
        set
        {
            _passwordPlaceholderVisible = value;
            OnPropertyChanged();
        }
    }

    private readonly RestClient _restClient;

    public IAsyncRelayCommand LoginCommand { get; }
    public IAsyncRelayCommand SignupCommand { get; }

    public LoginWindowViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !WorkingNow);
        SignupCommand = new AsyncRelayCommand(SignupAsync, () => !WorkingNow);
        _restClient = new RestClient(new RestClientOptions(App.EndPointUri));
    }

    [ObservableProperty] private bool _workingNow;
    private string _password = string.Empty;
    private bool _passwordPlaceholderVisible = true;

    private async Task LoginAsync(CancellationToken token = default)
    {
        WorkingNow = true;
        await AuthenticateAsync("api/user/login", "Logged in successfully", token);
        WorkingNow = false;
    }

    private async Task SignupAsync(CancellationToken token = default)
    {
        WorkingNow = true;
        await AuthenticateAsync("api/user/signup", "Signed up successfully", token);
        WorkingNow = false;
    }

    private void OpenMainWindow(string jwtToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(jwtToken);
        
        var currentWindow = Application.Current.MainWindow;
        _restClient.Dispose();

        new MainWindow(jwtToken).Show();
        currentWindow?.Close();
    }

    private async Task AuthenticateAsync(string endpoint, string successMessage, CancellationToken cancellationToken = default)
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        if (!ValidateInput(out var error))
        {
            ErrorMessage = error;
            return;
        }

        var request = new RestRequest(endpoint, Method.Post);
        request.AddBody(new {Username, PasswordHash = await HashPasswordAsync()});

        try
        {
            var response = await _restClient.PostAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                InfoMessage = successMessage;
                var jwtToken = JsonSerializer.Deserialize<string>(response.Content!);
                OpenMainWindow(jwtToken!);
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ErrorMessage = $"User {Username} not found";
            }
        }
        catch (HttpRequestException e)
        {
            ErrorMessage = e.StatusCode switch
            {
                HttpStatusCode.Conflict => $"Username {Username} is already taken",
                HttpStatusCode.Unauthorized => "Invalid username or password",
                null => "Unable to connect to the server",
                _ => $"Server error: {e.StatusCode}"
            };
        }
    }

    private bool ValidateInput(out string error)
    {
        error = string.Empty;
        
        var username = Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            error = "Username must not be empty";
            return false;
        }
        
        if (username.Length is < 2 or > 32)
        {
            error = "Username must be between 2 and 32 characters long";
            return false;
        }
        
        if (Password.Length is < 3 or > 64)
        {
            error = "Password must be between 3 and 64 characters long";
            return false;
        }
        
        return true;
    }

    [SuppressMessage("ReSharper.DPA", "DPA0001: Memory allocation issues")]
    private async Task<string> HashPasswordAsync()
    {
        // salting on server side
        var argon2 = new Argon2id(Encoding.UTF8.GetBytes(Password));
        argon2.DegreeOfParallelism = 8;
        argon2.MemorySize = 8192;
        argon2.Iterations = 8;
        argon2.AssociatedData = Encoding.UTF8.GetBytes(Username);

        var hash = await argon2.GetBytesAsync(64);
        return Convert.ToBase64String(hash);
    }
}