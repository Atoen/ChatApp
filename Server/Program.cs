using System.Net;
using System.Text;
using Serilog;
using Server;

Console.WriteLine("Server startup...");
Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File("log.txt").CreateLogger();

var address = IPAddress.Parse("127.0.0.1");
var endpoint = new IPEndPoint(address, 13000);

var server = new TcpServer(endpoint);
await server.Start();

await Task.Delay(Timeout.Infinite);
