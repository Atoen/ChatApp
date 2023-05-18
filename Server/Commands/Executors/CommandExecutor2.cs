namespace Server.Commands.Executors;

public class CommandExecutor2<T1, T2> : CommandExecutor
{
    private delegate Task Executor(T1 arg1, T2 arg2);

    private readonly Executor _executor;
    private readonly TypeReader _typeReader;
    private readonly bool _useRemainder;

    public CommandExecutor2(CommandInfo command, TypeReader typeReader) : base(command)
    {
        _typeReader = typeReader;
        _useRemainder = command.Parameters[1].IsRemainder;

        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(string[] args)
    {
        await _executor.Invoke(
            _typeReader.Read<T1>(args[0]),
            _typeReader.Read<T2>(_useRemainder
                ? string.Join(' ', args[1..])
                : args[1]));
    }
}