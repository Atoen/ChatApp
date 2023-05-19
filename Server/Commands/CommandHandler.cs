using System.Collections.Immutable;
using System.Diagnostics;
using Serilog;

namespace Server.Commands;

public class CommandHandler : ICommandHandler
{
    public CommandHandler(TcpServer server, char prefix, bool caseSensitive = false)
    {
        _server = server;
        _commandService = new CommandService();

        Prefix = prefix;
        CaseSensitive = caseSensitive;

        _modules = LoadModules();
        _commandService.RegisterCommands(_modules.SelectMany(x => x.Commands));
    }

    public char Prefix { get; set; }
    public bool CaseSensitive { get; set; }

    private readonly TcpServer _server;
    private readonly IReadOnlyList<ModuleInfo> _modules;
    private readonly CommandService _commandService;

    public async Task Handle(User sender, string command)
    {
        if (command.Length == 0) return;

        var result = await _commandService.Execute(sender, command);
        var response = result.Match(
            success => string.Empty,
            error => error.Value,
            notFound => $"Command '{command}' not found");

        if (response != string.Empty)
        {
            await _server.Respond(sender, response);
        }
    }

    private IReadOnlyList<ModuleInfo> LoadModules()
    {
        var start = Stopwatch.GetTimestamp();

        var moduleTypes = typeof(CommandHandler).Assembly.ExportedTypes.Where(type =>
                typeof(Module).IsAssignableFrom(type) && type is {IsAbstract: false});

        var modules = moduleTypes.Select(type =>
        {
            var constructor = type.GetConstructor(new[] {typeof(CommandService)});
            return constructor is null
                ? Activator.CreateInstance(type)
                : Activator.CreateInstance(type, _commandService);
        }).Cast<Module>().ToImmutableList();

        var moduleInfos = modules.Select(ModuleInfo.CreateModuleInfo).ToImmutableList();

        var end = Stopwatch.GetTimestamp();
        var timeSpan = TimeSpan.FromTicks(end - start);

        Log.Debug("Loaded {ModuleCount} module(s) in {Time}", moduleInfos.Count, timeSpan);

        return moduleInfos;
    }
}