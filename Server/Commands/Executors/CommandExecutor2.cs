namespace Server.Commands.Executors;

public class CommandExecutor2<T1, T2> : CommandParamExecutor
{
    private delegate Task Executor(CommandContext context, T1 arg1, T2 arg2);

    private readonly Executor _executor;

    public CommandExecutor2(CommandInfo command, TypeReader typeReader) : base(command, typeReader)
    {
        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(CommandContext context, string[] args)
    {
        await _executor.Invoke(context,
            TypeReader.Read<T1>(args[0]),
            TypeReader.Read<T2>(UseRemainder
                ? string.Join(' ', args[1..])
                : args[1]));
    }
}