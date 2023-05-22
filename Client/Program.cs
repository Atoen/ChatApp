using System.Text;
using OneOf.Types;
using Server;
using Server.Messages;

Console.OutputEncoding = Encoding.Unicode;

var client = new Client();

AppDomain.CurrentDomain.ProcessExit += async (_, _) => await client.CloseAsync();
Console.CancelKeyPress += async (_, _) => await client.CloseAsync();

Console.Write("Enter username: ");

var username = Console.ReadLine();
if (string.IsNullOrWhiteSpace(username)) username = "User";

async Task AnimateConnection(CancellationToken cancellationToken)
{
    var symbols = new[] {'\\', '|', '/', '-'};
    var index = 0;

    while (!cancellationToken.IsCancellationRequested)
    {
        Console.Write($"\rConnecting {symbols[index]}");
        await Task.Delay(200, cancellationToken);

        index = (index + 1) % symbols.Length;
    }
}

var tokenSource = new CancellationTokenSource();
_ = AnimateConnection(tokenSource.Token);

var result = await client.ConnectToServerAsync(username);
tokenSource.Cancel();
Console.Clear();

result.Switch(
    success => Console.WriteLine("Connected to the server."),
    changedName => Console.WriteLine($"Connected to the server as {changedName.Value}"),
    error =>
    {
        Console.WriteLine(error.Value);
        Console.Read();

        Environment.Exit(1);
    }
);

client.NotificationReceived += delegate(object? _, string notification)
{
    Console.WriteLine(notification);
};

client.MessageReceived += delegate(object? _, Message message)
{
    Console.WriteLine(message);
};

client.Listen();

string? message;
do
{
    message = Console.ReadLine() ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(message))
    {
        await client.SendMessageAsync(message);
    }
} while (message != "/exit");

await client.CloseAsync();
