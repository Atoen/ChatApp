using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Server;
using Server.Commands;
using Server.Net;

Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration().
    MinimumLevel.Debug().
    WriteTo.Console().
    CreateLogger();

var services = new ServiceCollection();
services.AddSingleton<ICommandHandler, CommandHandler>();
services.AddSingleton<CommandService>();
services.AddSingleton<FileTransferManager>();
services.AddLogging(x => x.AddSerilog(Log.Logger));

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
services.AddSingleton(httpClient);

var provider = services.BuildServiceProvider();

var address = IPAddress.Parse("192.168.1.108");
var endpoint = new IPEndPoint(address, 13000);

var handler = provider.GetRequiredService<ICommandHandler>();
var fileManager = provider.GetRequiredService<FileTransferManager>();
var server = new TcpServer(endpoint, handler, fileManager);
await server.Start().ConfigureAwait(false);

await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
