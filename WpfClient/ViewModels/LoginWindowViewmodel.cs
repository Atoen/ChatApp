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
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    private readonly RestClient _restClient;

    public IAsyncRelayCommand LoginCommand { get; }
    public IAsyncRelayCommand SignupCommand { get; }

    public LoginWindowViewModel()
    {
        LoginCommand = new AsyncRelayCommand(LoginAsync);
        SignupCommand = new AsyncRelayCommand(SignupAsync);
        _restClient = new RestClient(new RestClientOptions("https://localhost:7141"));
    }

    private async Task LoginAsync(CancellationToken token = default)
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;
        
        var isValid = ValidateInput();
        if (!isValid) return;
        
        var request = new RestRequest("api/user/login", Method.Post);
        request.AddBody(new {Username, PasswordHash = await HashPasswordAsync()}, ContentType.Json);

        try
        {
            var response = await _restClient.PostAsync(request, token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Message = "Logged in successfully";
                var jwtToken = JsonSerializer.Deserialize<string>(response.Content!)!;

                var currentWindow = Application.Current.MainWindow;
                _restClient.Dispose();
                
                new MainWindow(jwtToken).Show();
                currentWindow?.Close();
            }
        }
        catch (HttpRequestException e)
        {
            ErrorMessage = e.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Invalid username or password",
                _ => "Unable to connect to the server"
            };
        }
    }

    private async Task SignupAsync(CancellationToken token = default)
    {
        ErrorMessage = string.Empty;
        Message = string.Empty;
        
        var isValid = ValidateInput();
        if (!isValid) return;
        
        var request = new RestRequest("api/user/signup", Method.Post);
        request.AddBody(new {Username, PasswordHash = await HashPasswordAsync()}, ContentType.Json);

        try
        {
            var response = await _restClient.PostAsync(request, token);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Message = "Signed up successfully";
            }
            else
            {
                ErrorMessage = response.StatusCode.ToString();
            }
        }
        catch (HttpRequestException e)
        {
            ErrorMessage = e.StatusCode switch
            {
                HttpStatusCode.Conflict => $"Username {Username} is already taken",
                _ => "Unable to connect to the server"
            };
        }
    }

    private bool ValidateInput()
    {
        var username = Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            ErrorMessage = "Username must not be empty";
            return false;
        }
        
        if (username.Length is < 2 or > 32)
        {
            ErrorMessage = "Username must be between 2 and 32 characters long";
            return false;
        }
        
        if (Password.Length is < 3 or > 64)
        {
            ErrorMessage = "Password must be between 3 and 64 characters long";
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