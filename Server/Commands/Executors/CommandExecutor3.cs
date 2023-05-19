namespace Server.Commands.Executors;

public class CommandExecutor3<T1, T2, T3> : CommandParamExecutor
{
    private delegate Task Executor(CommandContext context, T1 arg1, T2 arg2, T3 arg3);

    private readonly Executor _executor;

    public CommandExecutor3(CommandInfo command, TypeReader typeReader) : base(command, typeReader)
    {
        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(CommandContext context, string[] args)
    {
        await _executor.Invoke(context,
            TypeReader.Read<T1>(args[0]),
            TypeReader.Read<T2>(args[1]),
            TypeReader.Read<T3>(UseRemainder
                ? string.Join(' ', args[2..])
                : args[2]));
    }
}