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

        try
        {
            _modules = LoadModules();
        }
        catch (CommandException e)
        {
            Log.Error("Invalid command present: {Error}", e.Message);
            throw;
        }
        catch (Exception e)
        {
            Log.Fatal("Error while creating modules: {Error}", e.Message);
            throw;
        }

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
        object? MatchConstructor(Type type)
        {
            var constructors = type.GetConstructors();
            foreach (var x in constructors)
            {
                var parameters = x.GetParameters();

                return parameters.Length switch
                {
                    0 => Activator.CreateInstance(type),
                    1 when parameters[0].ParameterType == typeof(CommandService) => Activator.CreateInstance(type,
                        _commandService),
                    _ => throw new ModuleException($"No matching constructor for {type} found.")
                };
            }

            throw new ModuleException($"No matching constructor for {type} found.");
        }

        var start = Stopwatch.GetTimestamp();

        var moduleTypes = typeof(CommandHandler).Assembly.ExportedTypes.Where(type =>
                typeof(Module).IsAssignableFrom(type) && type is {IsAbstract: false});

        var modules = moduleTypes.Select(MatchConstructor).Cast<Module>().ToImmutableList();

        var moduleInfos = modules.Select(ModuleInfo.CreateModuleInfo).ToImmutableList();

        var end = Stopwatch.GetTimestamp();
        var timeSpan = TimeSpan.FromTicks(end - start);

        Log.Debug("Loaded {ModuleCount} module(s) in {Time}", moduleInfos.Count, timeSpan);

        return moduleInfos;
    }
}