using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;
using Server.Commands.Executors;
using Server.Users;

namespace Server.Commands;

public class CommandService
{
    private readonly ILogger<CommandService> _logger;

    public CommandService(ILogger<CommandService> logger)
    {
        _logger = logger;
    }

    public bool CaseSensitive { get; set; }

    public IEnumerable<ModuleInfo> Modules => _modules.Select(x => x);
    public IEnumerable<CommandInfo> Commands => _modules.SelectMany(x => x.Commands);

    private readonly ConcurrentDictionary<string, CommandInfo> _commands = new();
    private readonly ConcurrentDictionary<CommandInfo, CommandExecutor> _commandExecutors = new();

    private readonly HashSet<ModuleInfo> _modules = new();
    private readonly HashSet<string> _registeredCommands = new();
    private readonly Dictionary<string, List<CommandInfo>> _registeredCommandsOverloads = new();
    private readonly ConcurrentDictionary<CommandInfo, CommandExecutor> _registeredExecutors = new();

    public void RegisterCommands(IEnumerable<CommandInfo> commands)
    {
        var start = Stopwatch.GetTimestamp();

        var typeReader = new TypeReader(CultureInfo.InvariantCulture);
        
        foreach (var command in commands)
        {
            _modules.Add(command.Module);
            foreach (var name in command.InvokeNames)
            {
                if (_registeredCommandsOverloads.TryGetValue(name, out var overloads))
                {
                    overloads.Add(command);
                }
                else
                {
                    _registeredCommandsOverloads.TryAdd(name, new List<CommandInfo> {command});
                }
            }

            _registeredExecutors.TryAdd(command, CreateExecutor(command, typeReader));
        }
        
        // AddCommands(commands);
        // AddExecutors();

        var stop = Stopwatch.GetTimestamp();
        var time = TimeSpan.FromTicks(stop - start);

        _logger.LogDebug("Successfully Registered {ExecutorCount} executors in {Time}", _registeredExecutors.Count, time);
    }

    private void AddCommands(IEnumerable<CommandInfo> commands)
    {
        var commandInfos = commands.ToList();
        foreach (var command in commandInfos)
        {
            _modules.Add(command.Module);
        }
        
        var dict = commandInfos.SelectMany(x => x.InvokeNames,
                (info, name) => new {info, name})
            .DistinctBy(arg => arg.name)
            .ToDictionary(x => x.name, x => x.info);
        
        foreach (var (key, val) in dict)
        {
            _logger.LogTrace("Registering {CommandName} command", key);
            if (!_commands.TryAdd(key, val))
            {
                _logger.LogWarning("Failed to register {CommandName} command", key);
            }
        }
    }
    
    private void AddExecutors()
    {
        var executors = CreateExecutors(_commands.Values.Distinct());
        foreach (var (key, val) in executors)
        {
            _logger.LogTrace("Registering {CommandName} executor", key.Name);
            if (!_commandExecutors.TryAdd(key, val))
            {
                _logger.LogWarning("Failed to register {CommandName} executor", key.Name);
            }
        }
    }

    public async Task<OneOf<Success, Error<string>, NotFound>> Execute(TcpUser tcpUser, string command)
    {
        // var args = command.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        // if (!_commands.TryGetValue(args[0], out var commandInfo))
        // {
        //     return new NotFound();
        // }

        var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var commandName = args[0];

        args = args[1..];

        if (!_registeredCommandsOverloads.TryGetValue(commandName, out var overloads))
        {
            return new NotFound();
        }

        var commandInfo = MatchOverload(args, overloads);
        
        if (commandInfo is null || !_registeredExecutors.TryGetValue(commandInfo, out var executor))
        {
            _logger.LogWarning("Unable to execute {Command}: executor not registered", commandName);
            return new Error<string>($"Unable to execute command '{commandName}'.");
        }

        var context = new CommandContext(tcpUser, args);

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

    private CommandInfo? MatchOverload(string[] args, List<CommandInfo> overloads)
    {
        if (overloads.Count == 1) return overloads[0];
        
        // foreach (var overload in overloads)
        // {
        //     // if (args.Length < overload.Parameters.Count)
        //     //     continue;
        //     //
        //     // bool match = true;
        //     // for (int i = 0; i < overload.Parameters.Count; i++)
        //     // {
        //     //     var parameter = overload.Parameters[i];
        //     //     var argument = args.ElementAtOrDefault(i);
        //     //
        //     //     if (argument == null)
        //     //     {
        //     //         match = false;
        //     //         break;
        //     //     }
        //     //
        //     //     var reader = new TypeReader(CultureInfo.InvariantCulture);
        //     //     try
        //     //     {
        //     //         var parsedArgument = reader.Read(argument, parameter.Type);
        //     //         // Additional checks or processing using the parsed argument and parameter
        //     //
        //     //         // Example: If the parameter has a default value and the argument is not provided,
        //     //         // you can set the parsedArgument to the default value
        //     //         if (parsedArgument == null && parameter.DefaultValue != null)
        //     //             parsedArgument = parameter.DefaultValue;
        //     //
        //     //         // Example: Add the parsed argument to a collection or modify the command's state accordingly
        //     //
        //     //     }
        //     //     catch (Exception)
        //     //     {
        //     //         match = false;
        //     //         break;
        //     //     }
        //     // }
        //     //
        //     // if (match)
        //     //     return overload;
        // }
        
        var typeReader = new TypeReader(CultureInfo.InvariantCulture);
        
        foreach (var overload in overloads.OrderBy(x => x.Priority))
        {
            if (args.Length < overload.Parameters.Count(x => !x.IsOptional)) continue;
        
            var match = true;
        
            try
            {
                for (var i = 0; i < overload.Parameters.Count; i++)
                {
                    var parameter = overload.Parameters[i];
                    var parsedArg = typeReader.Read(args[i], parameter.Type);
                }
            }
            catch (Exception e)
            {
                match = false;
            }
        
            if (match) return overload;
        }
        
        return overloads[0];
    }

    private Dictionary<CommandInfo, CommandExecutor> CreateExecutors(IEnumerable<CommandInfo> commands)
    {
        var reader = new TypeReader(CultureInfo.InvariantCulture);

        var dict = new Dictionary<CommandInfo, CommandExecutor>();
        foreach (var command in commands)
        {
            _logger.LogTrace("Creating executor for {Command} command", command.Name);
            var paramCount = command.Parameters.Count;

            try
            {
                var executor = paramCount == 0
                    ? new CommandExecutor0(command)
                    : CreateParamExecutor(paramCount, command, reader);

                dict.Add(command, executor);
            }
            catch (Exception e)
            {
                _logger.LogError("Error while creating executor for {Command} command: {Error}",
                    command.Name, e.InnerException?.Message ?? e.Message);

                throw;
            }
        }

        return dict;
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
}