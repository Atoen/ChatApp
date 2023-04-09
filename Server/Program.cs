using System.Net;
using Server;

Console.WriteLine("Server startup...");

var address = IPAddress.Parse("192.168.100.8");
var endpoint = new IPEndPoint(address, 13000);

var server = new TcpServer(endpoint);
await server.Start();

await Task.Delay(Timeout.Infinite);
