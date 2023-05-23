namespace Server.Commands.Executors;

public class CommandExecutor1<T> : CommandParamExecutor
{
    private delegate Task Executor(CommandContext context, T arg);

    private readonly Executor _executor;

    public CommandExecutor1(CommandInfo command, TypeReader typeReader) : base(command, typeReader)
    {
        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(CommandContext context, object[] args)
    {
        await _executor.Invoke(context, (T) args[0]).ConfigureAwait(false);
    }
}