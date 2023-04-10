using System.Net;
using Serilog;
using Server;

Console.WriteLine("Server startup...");

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File("log.txt").CreateLogger();

var address = IPAddress.Parse("192.168.100.8");
var endpoint = new IPEndPoint(address, 13000);

var server = new TcpServer(endpoint);
await server.Start();

await Task.Delay(Timeout.Infinite);
