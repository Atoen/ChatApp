using Microsoft.AspNetCore.SignalR.Client;

namespace Client;

public class SignalRClient
{
    private readonly string _username;
    private readonly HubConnection _connection;

    public SignalRClient(string username)
    {
        _username = username;
        _connection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7141/ChatHub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            Console.WriteLine($"{user}: {message}");
        });
        
        _connection.On<string>("UserJoined", user =>
        {
            Console.WriteLine($"{user}: joined");
        });
        
        _connection.On<string>("UserLeft", user =>
        {
            Console.WriteLine($"{user}: left");
        });
    }

    public async Task Connect()
    {
        try
        {
            await _connection.StartAsync();
            Console.WriteLine("Connection started");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task Send(string message)
    {
        try
        {
            await _connection.InvokeAsync("SendMessage", _username, message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}