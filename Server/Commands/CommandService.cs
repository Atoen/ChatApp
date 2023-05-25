using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using Server.Attributes;
using Server.Commands.Executors;
using Server.Users;

namespace Server.Commands;

public partial class CommandService
{
    private readonly ILogger<CommandService> _logger;

    public CommandService(ILogger<CommandService> logger)
    {
        _logger = logger;
    }

    public bool CaseSensitive { get; set; }

    public IEnumerable<ModuleInfo> Modules => _modules.Select(x => x);
    public IEnumerable<CommandInfo> Commands => _modules.SelectMany(x => x.Commands);

    private readonly HashSet<ModuleInfo> _modules = new();
    private readonly ConcurrentDictionary<string, List<CommandInfo>> _commands = new();
    private readonly ConcurrentDictionary<CommandInfo, CommandExecutor> _executors = new();

    public void RegisterCommands(IEnumerable<CommandInfo> commands)
    {
        var start = Stopwatch.GetTimestamp();
        var typeReader = new TypeReader(CultureInfo.InvariantCulture);
        
        foreach (var command in commands)
        {
            _modules.Add(command.Module);
            foreach (var name in command.InvokeNames)
            {
                if (_commands.TryGetValue(name, out var overloads))
                {
                    overloads.Add(command);
                }
                else
                {
                    _commands.TryAdd(name, new List<CommandInfo> {command});
                }
            }

            _executors.TryAdd(command, CreateExecutor(command, typeReader));
        }
        
        SortCommandInfos(_commands);

        var stop = Stopwatch.GetTimestamp();
        var time = TimeSpan.FromTicks(stop - start);

        _logger.LogDebug("Successfully Registered {ExecutorCount} executors in {Time}", _executors.Count, time);
    }

    private static readonly Regex ArgsRegex = ArgsSplitRegex();

    private static void SortCommandInfos(ConcurrentDictionary<string, List<CommandInfo>> dict)
    {
        foreach (var commandList in dict.Values)
        {
            commandList.Sort((x, y) =>
            {
                var priorityComparison = x.Priority.CompareTo(y.Priority);

                return priorityComparison != 0 ? priorityComparison :
                    y.Parameters.Count.CompareTo(x.Parameters.Count);
            });
        }
    }

    public async Task<OneOf<Success, Error<string>, NotFound>> Execute(TcpUser tcpUser, string command)
    {
        var tokens = ArgsRegex.Split(command).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

        if (tokens.Length == 0) return new NotFound();
        
        var commandName = tokens[0];
        var args = tokens[1..];

        if (!_commands.TryGetValue(commandName, out var overloads))
        {
            return new NotFound();
        }

        var commandInfo = MatchOverload(args, overloads, out var parsedArgs, out var message);

        if (commandInfo is null)
        {
            return new Error<string>(message!);
        }
        
        if (!_executors.TryGetValue(commandInfo, out var executor))
        {
            _logger.LogWarning("Unable to execute {Command}: executor not registered", commandName);
            return new Error<string>($"Unable to execute command '{commandName}'.");
        }

        var context = new CommandContext(tcpUser, parsedArgs);

        _logger.LogDebug("Executing {CommandName} for {Username}", command, tcpUser.Username);

        try
        {
            await executor.Execute(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Error when executing {CommandName}: {Error}", commandName, e.Message);
            return new Error<string>(e.Message);
        }

        return new Success();
    }

    private CommandInfo? MatchOverload(string[] args, List<CommandInfo> overloads, out object[] parsedArgs, out string? message)
    {
        parsedArgs = Array.Empty<object>();
        message = null;

        if (overloads.Count == 1)
        {
            var commandInfo = overloads[0];

            if (!ValidateArgsCount(args, ref message, commandInfo)) return null;
            var result = ParseArgs(commandInfo, args);

            if (result.IsT0)
            {
                parsedArgs = result.AsT0;
                return commandInfo;
            }

            message = result.AsT1.Value;
            return null;
        }

        foreach (var overload in overloads.OrderBy(x => x.Priority))
        {
            if (args.Length < overload.Parameters.Count(x => !x.IsOptional)) continue;

            if (!ValidateArgsCount(args, ref message, overload)) continue;
            var result = ParseArgs(overload, args);

            if (result.IsT0)
            {
                parsedArgs = result.AsT0;
                return overload;
            }
        }

        message = "No matching overload found.";
        return null;
    }

    private static bool ValidateArgsCount(string[] args, ref string? message, CommandInfo commandInfo)
    {
        if (args.Length < commandInfo.RequiredParamCount)
        {
            message = "Too few arguments provided.";
            return false;
        }

        if (args.Length > commandInfo.Parameters.Count &&
            commandInfo.ExtraArgsHandleMode == ExtraArgsHandleMode.Throw)
        {
            message = "Too many arguments provided.";
            return false;
        }

        return true;
    }

    private OneOf<object[], Error<string>> ParseArgs(CommandInfo commandInfo, string[] args)
    {
        var typeReader = new TypeReader(CultureInfo.InvariantCulture);
        var parametersCount = commandInfo.Parameters.Count;
        var parsedArgs = new object[parametersCount];

        for (var i = 0; i < args.Length; i++)
        {
            if (i >= parametersCount) break;

            var parameter = commandInfo.Parameters[i];
            var toRead = parameter.IsRemainder
                ? string.Join(' ', args[i..])
                : args[i];

            try
            {
                parsedArgs[i] = typeReader.Read(toRead, parameter);
            }
            catch
            {
                return new Error<string>(
                    $"Arg '{args[i]}' is in invalid format. Expected {parameter.Type.Name}");
            }
        }

        for (var i = args.Length; i < parametersCount; i++)
        {
            parsedArgs[i] = commandInfo.Parameters[i].DefaultValue!;
        }

        return parsedArgs;
    }

    private CommandExecutor CreateExecutor(CommandInfo command, TypeReader typeReader)
    {
        _logger.LogTrace("Creating executor for {Command} command", command.Name);
        var paramCount = command.Parameters.Count;

        try
        {
            var executor = paramCount == 0
                ? new CommandExecutor0(command)
                : CreateParamExecutor(paramCount, command, typeReader);

            return executor;
        }
        catch (Exception e)
        {
            _logger.LogError("Error while creating executor for {Command} command: {Error}",
                command.Name, e.InnerException?.Message ?? e.Message);

            throw;
        }
    }

    private static CommandExecutor CreateParamExecutor(int parameterCount, CommandInfo command, TypeReader reader)
    {
        var executorType = parameterCount switch
        {
            1 => typeof(CommandExecutor1<>),
            2 => typeof(CommandExecutor2<,>),
            3 => typeof(CommandExecutor3<,,>),
            _ => throw new ArgumentOutOfRangeException(nameof(parameterCount), parameterCount, null)
        };

        var types = command.Parameters.Select(x => x.Type).ToArray();
        var genericExecutorType = executorType.MakeGenericType(types);

        var executor = (CommandExecutor) Activator.CreateInstance(genericExecutorType, command, reader)!;

        return executor;
    }

    [GeneratedRegex("\\s+(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled)]
    private static partial Regex ArgsSplitRegex();
}