using System.Collections.Concurrent;
using OneOf;
using OneOf.Types;
using Serilog;

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
        
        var executors = CreateExecutors(_commands.Values.Distinct());
        foreach (var (key, val) in executors)
        {
            Log.Verbose("Registering {CommandName} executor", key.Name);
            if (!_commandExecutors.TryAdd(key, val))
            {
                Log.Warning("Failed to register {CommandName} executor", key.Name);
            }
        }
        
        Log.Debug("Successfully Registered {ExecutorCount} executors", _commandExecutors.Count);
    }

    public async Task<OneOf<Success, Error<string>, NotFound>> Execute(User user, string command)
    {
        var args = command.Split(' ');
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
            Log.Warning("Error when executing {CommandName}: {Error}", args[0], e.Message);
            return new Error<string>(e.Message);
        }

        return new Success();
    }

    private static Dictionary<CommandInfo, CommandExecutor> CreateExecutors(IEnumerable<CommandInfo> commands)
    {
        var dict = new Dictionary<CommandInfo, CommandExecutor>();
        foreach (var command in commands)
        {
            switch (command.Parameters.Count)
            {
                case 0:
                    dict.Add(command, new CommandExecutor0(command));
                    break;
                
                case 1:
                    dict.Add(command, new CommandExecutor1(command));
                    break;
            }
        }

        return dict;
    }
}