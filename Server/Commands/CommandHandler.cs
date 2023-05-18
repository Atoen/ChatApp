using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Serilog;
using Server.Attributes;

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

        var moduleInfos = modules.Select(CreateModuleInfo).ToImmutableList();

        var end = Stopwatch.GetTimestamp();
        var timeSpan = TimeSpan.FromTicks(end - start);

        Log.Debug("Loaded {ModuleCount} module(s) in {Time}", moduleInfos.Count, timeSpan);

        return moduleInfos;
    }

    private static ModuleInfo CreateModuleInfo(Module module)
    {
        var moduleInfo = new ModuleInfo(module);

        var attributes = module.GetType().GetCustomAttributes();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case NameAttribute name:
                    moduleInfo.Name = name.Text;
                    break;

                case SummaryAttribute summary:
                    moduleInfo.Summary = summary.Text;
                    break;

                default:
                    moduleInfo.AddAttribute(attribute);
                    break;
            }
        }

        if (moduleInfo.Name == string.Empty)
        {
            moduleInfo.Name = module.GetType().Name;
        }

        var methods = from methodInfo in module.GetType().GetMethods()
            let commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>()
            where commandAttribute is not null
            select methodInfo;

        var commands = methods.Select(x => CreateCommandInfo(x, moduleInfo));
        moduleInfo.AddCommands(commands);

        Log.Debug("Loaded {ModuleName} module", moduleInfo.Name);

        return moduleInfo;
    }

    private static CommandInfo CreateCommandInfo(MethodInfo method, ModuleInfo module)
    {
        var commandInfo = new CommandInfo(method)
        {
            Module = module
        };

        var attributes = method.GetCustomAttributes();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case CommandAttribute command:
                    if (commandInfo.Name == string.Empty) commandInfo.Name = command.Text;
                    break;

                case NameAttribute name:
                    commandInfo.Name = name.Text;
                    break;

                case SummaryAttribute summary:
                    commandInfo.Summary = summary.Text;
                    break;

                case AliasAttribute alias:
                    commandInfo.WithAlias(alias.Aliases);
                    break;

                default:
                    commandInfo.AddAttribute(attribute);
                    break;
            }
        }

        var parameters = method.GetParameters().Select(x => CreateParameterInfo(x, commandInfo));
        commandInfo.AddParameters(parameters);

        return commandInfo;
    }

    private static ParameterInfo CreateParameterInfo(System.Reflection.ParameterInfo parameter, CommandInfo command)
    {
        var parameterInfo = new ParameterInfo
        {
            Type = parameter.ParameterType,
            Command = command,
            IsOptional = parameter.IsOptional,
            DefaultValue = parameter.DefaultValue
        };

        var attributes = parameter.GetCustomAttributes().ToImmutableArray();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case NameAttribute name:
                    parameterInfo.Name = name.Text;
                    break;

                case AliasAttribute alias:
                    parameterInfo.WithAlias(alias.Aliases);
                    break;

                case SummaryAttribute summary:
                    parameterInfo.Summary = summary.Text;
                    break;

                case RemainderAttribute:
                    parameterInfo.IsRemainder = true;
                    break;

                default:
                    parameterInfo.AddAttribute(attribute);
                    break;
            }
        }

        return parameterInfo;
    }
}