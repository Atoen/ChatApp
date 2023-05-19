using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using OneOf;
using OneOf.Types;
using Serilog;
using Server.Commands.Executors;

namespace Server.Commands;

public class CommandService
{
    public bool CaseSensitive { get; set; }

    public IEnumerable<ModuleInfo> Modules => _modules.Select(x => x);
    public IEnumerable<CommandInfo> Commands => _modules.SelectMany(x => x.Commands);

    private readonly HashSet<ModuleInfo> _modules = new();
    private readonly ConcurrentDictionary<string, CommandInfo> _commands = new();
    private readonly ConcurrentDictionary<CommandInfo, CommandExecutor> _commandExecutors = new();

    public void RegisterCommands(IEnumerable<CommandInfo> commands)
    {
        var start = Stopwatch.GetTimestamp();

        AddCommands(commands);
        AddExecutors();

        var stop = Stopwatch.GetTimestamp();
        var time = TimeSpan.FromTicks(stop - start);

        Log.Debug("Successfully Registered {ExecutorCount} executors in {Time}", _commandExecutors.Count, time);
    }

    private void AddExecutors()
    {
        var executors = CreateExecutors(_commands.Values.Distinct());
        foreach (var (key, val) in executors)
        {
            Log.Verbose("Registering {CommandName} executor", key.Name);
            if (!_commandExecutors.TryAdd(key, val))
            {
                Log.Warning("Failed to register {CommandName} executor", key.Name);
            }
        }
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
            .ToDictionary(x => x.name, x => x.info);

        foreach (var (key, val) in dict)
        {
            Log.Verbose("Registering {CommandName} command", key);
            if (!_commands.TryAdd(key, val))
            {
                Log.Warning("Failed to register {CommandName} command", key);
            }
        }
    }

    public async Task<OneOf<Success, Error<string>, NotFound>> Execute(User user, string command)
    {
        var args = command.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        if (!_commands.TryGetValue(args[0], out var commandInfo))
        {
            return new NotFound();
        }

        if (!_commandExecutors.TryGetValue(commandInfo, out var executor))
        {
            Log.Warning("Unable to execute {Command}: executor not registered", args[0]);
            return new Error<string>($"Unable to execute command '{args[0]}'.");
        }

        var context = new CommandContext(user, args[1..]);

        Log.Debug("Executing {CommandName} for {Username}", command, user.Username);

        try
        {
            await executor.Execute(context);
        }
        catch (Exception e)
        {
            Log.Information("Error when executing {CommandName}: {Error}", args[0], e.Message);
            return new Error<string>(e.Message);
        }

        return new Success();
    }

    private static Dictionary<CommandInfo, CommandExecutor> CreateExecutors(IEnumerable<CommandInfo> commands)
    {
        var reader = new TypeReader(CultureInfo.InvariantCulture);

        var dict = new Dictionary<CommandInfo, CommandExecutor>();
        foreach (var command in commands)
        {
            Log.Verbose("Creating executor for {Command} command", command.Name);
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
                Log.Error("Error while creating executor for {Command} command: {Error}",
                    command.Name, e.InnerException?.Message ?? e.Message);

                throw;
            }
        }

        return dict;
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