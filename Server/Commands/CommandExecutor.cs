using Server.Modules;

namespace Server.Commands;

public class CommandExecutor0 : ICommandExecutor
{
    private delegate Task Executor0();

    private readonly Executor0 _executor0;
    private readonly CommandInfo _commandInfo;

    public CommandExecutor0(CommandInfo command)
    {
        _commandInfo = command;
        _executor0 = (Executor0) Delegate.CreateDelegate(typeof(Executor0), command.Module.Module, command.Method);
    }

    public Task Execute(CommandContext context)
    {
        _commandInfo.Module.Module.SetContext(context);
        return _executor0.Invoke();
    }
}

public class CommandExecutor1 : ICommandExecutor
{
    private delegate Task Executor1(string arg);
    
    private readonly Executor1 _executor1;
    private readonly CommandInfo _commandInfo;

    public CommandExecutor1(CommandInfo command)
    {
        _commandInfo = command;
        _executor1 = (Executor1) Delegate.CreateDelegate(typeof(Executor1), command.Module.Module, command.Method);
    }
    
    public Task Execute(CommandContext context)
    {
        _commandInfo.Module.Module.SetContext(context);
        return _executor1.Invoke(context.Args[0]);
    }
}