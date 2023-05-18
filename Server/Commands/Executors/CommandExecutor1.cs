namespace Server.Commands.Executors;

public class CommandExecutor1<T> : CommandExecutor
{
    private delegate Task Executor(T arg);

    private readonly Executor _executor;
    private readonly TypeReader _typeReader;
    private readonly bool _useRemainder;

    public CommandExecutor1(CommandInfo command, TypeReader typeReader) : base(command)
    {
        _typeReader = typeReader;
        _useRemainder = command.Parameters[0].IsRemainder;

        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(string[] args)
    {
        await _executor.Invoke(_typeReader.Read<T>(_useRemainder
            ? string.Join(' ', args)
            : args[0]));
    }
}