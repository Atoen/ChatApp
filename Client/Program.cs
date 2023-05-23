using System.Text;
using Server;
using Server.Messages;

Console.OutputEncoding = Encoding.Unicode;

Console.Clear();


var client = new Client();

AppDomain.CurrentDomain.ProcessExit += async (_, _) => await client.CloseAsync().ConfigureAwait(false);
Console.CancelKeyPress += async (_, _) => await client.CloseAsync().ConfigureAwait(false);

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
        await Task.Delay(200, cancellationToken).ConfigureAwait(false);

        index = (index + 1) % symbols.Length;
    }
}

var tokenSource = new CancellationTokenSource();
_ = AnimateConnection(tokenSource.Token);

var result = await client.ConnectToServerAsync(username).ConfigureAwait(false);
tokenSource.Cancel();
Console.Clear();

result.Switch(
    success => Console.WriteLine($"Connected to the server as {success.Value}."),
    error =>
    {
        Console.WriteLine(error.Value);
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
        await client.SendMessageAsync(message).ConfigureAwait(false);
    }
} while (message != "/exit");

await client.CloseAsync().ConfigureAwait(false);
