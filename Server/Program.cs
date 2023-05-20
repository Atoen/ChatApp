﻿using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Server;
using Server.Commands;

Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration().
    MinimumLevel.Debug().
    WriteTo.Console().
    CreateLogger();

var services = new ServiceCollection();
services.AddSingleton<ICommandHandler, CommandHandler>();
services.AddSingleton<CommandService>();
services.AddSingleton<HttpClient>();
services.AddLogging(x => x.AddSerilog(Log.Logger));

var provider = services.BuildServiceProvider();

var address = IPAddress.Parse("127.0.0.1");
var endpoint = new IPEndPoint(address, 13000);

var handler = provider.GetRequiredService<ICommandHandler>();
var server = new TcpServer(endpoint, handler);
await server.Start();

await Task.Delay(Timeout.Infinite);
