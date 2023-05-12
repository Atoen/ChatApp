using Server;

var client = new Client();

AppDomain.CurrentDomain.ProcessExit += delegate
{
    client.Close();
};

Console.Write("Enter username: ");

var username = Console.ReadLine() ?? string.Empty;

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
    message = Console.ReadLine();
    
    var (left, top) = Console.GetCursorPosition();
    Console.SetCursorPosition(left, top - 1);
    
    if (!string.IsNullOrWhiteSpace(message)) await client.SendMessageAsync(message);
} while (message != "/exit");

await Task.Delay(Timeout.Infinite);




