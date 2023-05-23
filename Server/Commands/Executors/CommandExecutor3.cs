namespace Server.Commands.Executors;

public class CommandExecutor3<T1, T2, T3> : CommandParamExecutor
{
    private delegate Task Executor(CommandContext context, T1 arg1, T2 arg2, T3 arg3);

    private readonly Executor _executor;

    public CommandExecutor3(CommandInfo command, TypeReader typeReader) : base(command, typeReader)
    {
        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(CommandContext context, object[] args)
    {
        await _executor.Invoke(context, (T1) args[0], (T2) args[1], (T3) args[2]).ConfigureAwait(false);
    }
}