using Server.Modules;

namespace Server.Commands;

public class CommandExecutor0 : CommandExecutor
{
    private delegate Task Executor0();

    private readonly Executor0 _executor;

    public CommandExecutor0(CommandInfo command) : base(command)
    {
        _executor = (Executor0) Delegate.CreateDelegate(typeof(Executor0), command.Module.Instance, command.Method);
    }
    
    protected override async Task Invoke(string[] args) => await _executor.Invoke();
}

public class CommandExecutor1 : CommandExecutor
{
    private delegate Task Executor1(string arg);
    
    private readonly Executor1 _executor;

    public CommandExecutor1(CommandInfo command) : base(command)
    {
        _executor = (Executor1) Delegate.CreateDelegate(typeof(Executor1), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(string[] args)
    {
        if (CommandInfo.Parameters[0].IsRemainder)
        {
            await _executor.Invoke(string.Join(' ', args));
        }
        else
        {
            await _executor.Invoke(args[0]);
        }
    }
}