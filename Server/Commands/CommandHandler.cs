using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Exceptions;
using Server.Messages;
using Server.Users;

namespace Server.Commands;

public class CommandHandler : ICommandHandler
{
    private readonly CommandService _commandService;
    private readonly IServiceProvider _provider;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(CommandService commandService, ILogger<CommandHandler> logger, IServiceProvider provider)
    {
        _commandService = commandService;
        _provider = provider;
        _logger = logger;

        Prefix = '/';
        CaseSensitive = false;

        _modules = LoadModules();

        _commandService.RegisterCommands(_modules.SelectMany(x => x.Commands));
    }

    public static readonly User CommandResponder = new("Server", Guid.Empty);

    private IReadOnlyList<ModuleInfo> LoadModules()
    {
        try
        {
            return BuildModules(_provider);
        }
        catch (CommandException e)
        {
            _logger.LogError("Invalid command present: {Error}", e.Message);
            throw;
        }
        catch (Exception e)
        {
            _logger.LogCritical("Error while creating modules: {Error}", e.Message);
            throw;
        }
    }

    public char Prefix { get; set; }
    public bool CaseSensitive { get; set; }

    private readonly IReadOnlyList<ModuleInfo> _modules;

    public async Task Handle(TcpUser user, string command)
    {
        if (command.Length == 0) return;

        var result = await _commandService.Execute(user, command);
        var response = result.Match(
            success => string.Empty,
            error => error.Value,
            notFound => $"Command '{command}' not found");

        if (response != string.Empty)
        {
            await user.WriteMessageAsync(Message.ServerNotification(response));
        }
    }

    private IReadOnlyList<ModuleInfo> BuildModules(IServiceProvider provider)
    {
        var start = Stopwatch.GetTimestamp();

        var moduleTypes = typeof(CommandHandler).Assembly.ExportedTypes.Where(type =>
                typeof(Module).IsAssignableFrom(type) && !type.IsAbstract);

        var modules = moduleTypes.Select(x => ActivatorUtilities.CreateInstance(provider, x))
            .Cast<Module>().ToImmutableList();

        var moduleInfos = modules.Select(ModuleInfo.CreateModuleInfo).ToImmutableList();

        var end = Stopwatch.GetTimestamp();
        var timeSpan = TimeSpan.FromTicks(end - start);

        _logger.LogDebug("Loaded {ModuleCount} module(s) in {Time}", moduleInfos.Count, timeSpan);

        return moduleInfos;
    }
}