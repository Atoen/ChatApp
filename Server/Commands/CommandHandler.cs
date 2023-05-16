using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Serilog;
using Server.Attributes;
using Server.Modules;
using Module = Server.Modules.Module;
using ParameterInfo = Server.Modules.ParameterInfo;

namespace Server.Commands;

public class CommandHandler : ICommandHandler
{
    public CommandHandler(TcpServer server, char prefix, bool caseSensitive = false)
    {
        _server = server;

        Prefix = prefix;
        CaseSensitive = caseSensitive;

        _modules = LoadModules();
    }

    public async Task Handle(User sender, string command)
    {
        var commandBody = command[1..];
        var args = commandBody.Split(' ');

        if (args.Length < 1) return;
        if (!CaseSensitive) args[0] = args[0].ToLower();

        if (!Handlers.TryGetValue(args[0], out var handler))
        {
            await _server.Respond(sender, $"Command \"{args[0]}\" not found");
            return;
        }

        try
        {
            Log.Debug("Executing '{Command}' for {User}", commandBody, sender.Username);
            await handler.Invoke(sender, args[1..]);
        }
        catch (Exception e)
        {
            Log.Information("Error while executing {Command} for {User}: {Error}",
                command, sender.Username, e.Message);

            await _server.Respond(sender, e.Message);
        }
    }

    public char Prefix { get; set; }
    public bool CaseSensitive { get; set; }
    public Dictionary<string, CommandExecutionContext> Handlers { get; } = new();

    private readonly TcpServer _server;
    private readonly IReadOnlyList<ModuleInfo> _modules;

    private static IReadOnlyList<ModuleInfo> LoadModules()
    {
        var start = Stopwatch.GetTimestamp();

        var modules = typeof(CommandHandler).Assembly.ExportedTypes.Where(type =>
            typeof(Module).IsAssignableFrom(type) && type is {IsAbstract: false}).
            Select(Activator.CreateInstance).Cast<Module>().ToList();

        var moduleInfos = modules.Select(CreateModuleInfo).ToImmutableList();

        var end = Stopwatch.GetTimestamp();
        var timeSpan = TimeSpan.FromTicks(end - start);

        Log.Debug("Loaded {ModuleCount} module(s) in {Time}", moduleInfos.Count, timeSpan);

        return moduleInfos;
    }

    private static ModuleInfo CreateModuleInfo(Module module)
    {
        var moduleInfo = new ModuleInfo();

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
        var commandInfo = new CommandInfo
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