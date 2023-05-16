using System.Net;
using System.Text;
using Serilog;
using Server;

Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration().
    MinimumLevel.Debug().
    WriteTo.Console().
    CreateLogger();

var address = IPAddress.Parse("192.168.1.108");
var endpoint = new IPEndPoint(address, 13000);

var server = new TcpServer(endpoint);
await server.Start();

await Task.Delay(Timeout.Infinite);
