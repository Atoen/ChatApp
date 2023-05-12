using System.Net;
using System.Text;
using Serilog;
using Server;

Console.OutputEncoding = Encoding.UTF8;

var address = IPAddress.Parse("127.0.0.1");
var endpoint = new IPEndPoint(address, 13000);

var server = new TcpServer(endpoint);
await server.Start();

await Task.Delay(Timeout.Infinite);
