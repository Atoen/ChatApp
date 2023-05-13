using Server;

var client = new Client();

AppDomain.CurrentDomain.ProcessExit += (_, _) => client.Close();
Console.CancelKeyPress += (_, _) => client.Close();

Console.Write("Enter username: ");

var username = Console.ReadLine() ?? "User";

Console.WriteLine("Connecting...");
var result = await client.ConnectToServerAsync(username);
result.Switch(
    success => Console.Clear(),
    error => Console.WriteLine(error.Value)
);

client.MessageReceived += delegate(object? _, Message message)
{
    Console.WriteLine(message);
};

client.Listen();

string? message;
do
{
    message = Console.ReadLine() ?? string.Empty;
    
    Console.CursorTop--;
    Console.Write($"\r{new string(' ', message.Length)}");
    Console.CursorLeft = 0;

    if (!string.IsNullOrWhiteSpace(message)) await client.SendMessageAsync(message);
} while (message != "/exit");

await Task.Delay(Timeout.Infinite);




